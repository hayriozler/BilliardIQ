using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scoreboard.Client.Models;
using System.Text.Json;

namespace Scoreboard.Client.Services;

public partial class RemotePullService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    IOptions<RemoteSyncOptions> options,
    SystemPowerService systemPower,
    LanguageSync languageSync,
    ServerClock clock,
    StartupGate startup,
    ILogger<RemotePullService> logger) : BackgroundService
{
    private const string _photosFolder = "PlayerSet";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RemoteSyncOptions _options = options.Value;
    private bool _resync = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await startup.WaitAsync(stoppingToken);

            if (!_options.Enabled)
            {
                LogPullDisabled();
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                LogPullNoBaseUrl();
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                LogPullNoClientId();
                return;
            }

            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            var http = httpClientFactory.CreateClient(nameof(RemotePullService));
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);
            if (_options.TableNo > 0)
            {
                http.DefaultRequestHeaders.Add("X-Table-No", _options.TableNo.ToString());
            }

            http.DefaultRequestHeaders.Add("X-Client-Instance", await GetInstanceIdAsync(stoppingToken));

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await PullAsync(http, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogPullTickFailed(ex);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogPullStartFailed(ex);
        }
    }

    private async Task PullAsync(HttpClient http, CancellationToken ct)
    {
        if (systemPower.IsShuttingDown)
        {
            return;
        }

        LogSendGet();
        var organizationPulled = await IsOrganizationPulledAsync(ct);
        if (organizationPulled && !await HasTimeZoneAsync(ct))
        {
            await PullTimeZoneAsync(http, ct);
        }

        RemoteOrganization? organization = null;
        RemoteChanges changes;
        try
        {
            if (!organizationPulled)
            {
                organization = await http.GetFromJsonAsync<RemoteOrganization>("scoreboard/organization", _jsonOptions, ct);
                if (organization is null)
                {
                    var ex = new InvalidOperationException("Organization data is null");
                    LogReceivedFailed("Organization data is null", ex);
                    return;
                }
            }
            using var changesRequest = new HttpRequestMessage(HttpMethod.Get, $"scoreboard/changes?resync={_resync}");
            if (LocalNetwork.GetIPv4() is { } localIp)
            {
                changesRequest.Headers.Add("X-Client-Ip", localIp);
            }

            using var changesResponse = await http.SendAsync(changesRequest, ct);
            changesResponse.EnsureSuccessStatusCode();
            changes = await changesResponse.Content.ReadFromJsonAsync<RemoteChanges>(_jsonOptions, ct)
                ?? throw new HttpRequestException("Change list is null");
        }
        catch (HttpRequestException e)
        {
            _resync = true;
            LogReceivedFailed(e.Message, e);
            return;
        }

        var clubs = changes.ItemsOf<RemoteClub>("Club");
        var teams = changes.ItemsOf<RemoteTeam>("Team");
        var players = changes.ItemsOf<RemotePlayer>("Player");
        var nothingChanged = !changes.Full && changes.Changes.Count == 0;
        if (organizationPulled && nothingChanged)
        {
            LogNoChanges();
            return;
        }

        LogReceived(clubs.Count, teams.Count, players.Count);

        var photoPaths = await DownloadPhotosAsync(http, players, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var languageChanged = false;
        if (!organizationPulled)
        {
            languageChanged = await ApplyServerLanguageAsync(db, organization!.Language, ct);
            await ApplyOrganizationNameAsync(db, organization.Name, ct);
            await ApplyTimeZoneAsync(db, organization.TimeZone, ct);
            await MarkOrganizationPulledAsync(db, ct);
        }

        await UpsertClubsAsync(db, clubs, changes.Full, changes.DeletedIdsOf("Club"), ct);
        await UpsertTeamsAsync(db, teams, changes.Full, changes.DeletedIdsOf("Team"), ct);
        await UpsertPlayersAsync(db, photoPaths, players, teams, changes.Full, changes.DeletedIdsOf("Player"), ct);

        await transaction.CommitAsync(ct);
        if (organization is not null)
        {
            clock.UseTimeZone(organization.TimeZone);
        }

        _resync = false;

        if (languageChanged)
        {
            LocalizationService.InvalidateLanguage();
            languageSync.Notify();
        }

        await AcknowledgeAsync(http, ct);
    }

    private async Task<string> GetInstanceIdAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.SettingsSet.AsNoTracking().Where(s => s.Id == "InstanceId").Select(s => s.Value).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var created = Guid.NewGuid().ToString("N");
        await SetSettingAsync(db, "InstanceId", created, ct);
        return created;
    }

    private async Task AcknowledgeAsync(HttpClient http, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync("scoreboard/changes/ack", null, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            LogAckFailed(ex.Message, ex);
        }
    }

    private static async Task SetSettingAsync(DataContext db, string key, string value, CancellationToken ct)
    {
        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == key, ct);
        if (setting is null)
        {
            db.SettingsSet.Add(new Setting { Id = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> HasTimeZoneAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.SettingsSet.AsNoTracking().AnyAsync(s => s.Id == "TimeZone", ct);
    }

    private async Task PullTimeZoneAsync(HttpClient http, CancellationToken ct)
    {
        try
        {
            var organization = await http.GetFromJsonAsync<RemoteOrganization>("scoreboard/organization", _jsonOptions, ct);
            if (organization is null)
            {
                return;
            }

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await ApplyTimeZoneAsync(db, organization.TimeZone, ct);
            clock.UseTimeZone(organization.TimeZone);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            LogTimeZonePullFailed(ex);
        }
    }

    private static async Task ApplyTimeZoneAsync(DataContext db, string? timeZone, CancellationToken ct)
    {
        var value = timeZone?.Trim() ?? "";
        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == "TimeZone", ct);
        if (setting is null)
        {
            db.SettingsSet.Add(new Setting { Id = "TimeZone", Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read the organization time zone; the system time zone is used for now.")]
    private partial void LogTimeZonePullFailed(Exception ex);

    private async Task<bool> IsOrganizationPulledAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pulledFor = await db.SettingsSet.AsNoTracking().Where(s => s.Id == "OrgClientId").Select(s => s.Value).FirstOrDefaultAsync(ct);
        return pulledFor == _options.ClientId;
    }

    private async Task MarkOrganizationPulledAsync(DataContext db, CancellationToken ct)
    {
        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == "OrgClientId", ct);
        if (setting is null)
        {
            db.SettingsSet.Add(new Setting { Id = "OrgClientId", Value = _options.ClientId });
        }
        else
        {
            setting.Value = _options.ClientId;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task ApplyOrganizationNameAsync(DataContext db, string orgName, CancellationToken ct)
    {
        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == "OrgName", ct);
        if ((setting?.Value ?? "") == orgName)
        {
            return;
        }

        if (setting is null)
        {
            db.SettingsSet.Add(new Setting { Id = "OrgName", Value = orgName });
        }
        else
        {
            setting.Value = orgName;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<bool> ApplyServerLanguageAsync(DataContext db, string? language, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(language) || !LocalizationService.Values.ContainsKey(language))
        {
            return false;
        }

        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == "Lang", ct);
        if (setting is null || setting.Value == language)
        {
            return false;
        }

        setting.Value = language;
        await db.SaveChangesAsync(ct);
        LocalizationService.RenameSystemPlayers(db, language);
        return true;
    }

    private static async Task UpsertClubsAsync(DataContext db, List<RemoteClub> remoteClubs, bool prune, HashSet<int> deletedIds, CancellationToken ct)
    {
        var local = await db.ClubSet.ToDictionaryAsync(c => c.Id, ct);
        foreach (var remote in remoteClubs)
        {
            if (!local.TryGetValue(remote.Id, out var club))
            {
                club = new Club { Id = remote.Id };
                db.ClubSet.Add(club);
                local[remote.Id] = club;
            }

            club.Name = remote.Name;
            club.ShortName = remote.ShortName;
            club.City = remote.City;
            club.PrimaryColor = remote.PrimaryColor;
        }

        var remoteIds = remoteClubs.Select(c => c.Id).ToHashSet();
        db.ClubSet.RemoveRange(local.Values.Where(c => prune ? !remoteIds.Contains(c.Id) : deletedIds.Contains(c.Id)).ToList());
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertTeamsAsync(DataContext db, List<RemoteTeam> remoteTeams, bool prune, HashSet<int> deletedIds, CancellationToken ct)
    {
        var local = await db.TeamSet.ToDictionaryAsync(t => t.Id, ct);
        foreach (var remote in remoteTeams)
        {
            if (!local.TryGetValue(remote.Id, out var team))
            {
                team = new Team { Id = remote.Id };
                db.TeamSet.Add(team);
                local[remote.Id] = team;
            }

            team.Name = remote.Name;
            team.ClubId = remote.ClubId;
            team.UpdatedAt = remote.UpdatedAt;
            team.AvatarId = remote.AvatarId;
        }

        var remoteIds = remoteTeams.Select(t => t.Id).ToHashSet();
        db.TeamSet.RemoveRange(local.Values.Where(t => prune ? !remoteIds.Contains(t.Id) : deletedIds.Contains(t.Id)).ToList());
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertPlayersAsync(
        DataContext db, Dictionary<int, string?> photoPaths, List<RemotePlayer> remotePlayers, List<RemoteTeam> remoteTeams, bool prune, HashSet<int> deletedIds, CancellationToken ct)
    {
        var local = await db.PlayerSet.ToDictionaryAsync(p => p.Id, ct);
        var remoteIds = remotePlayers.Select(p => p.Id).ToHashSet();

        var serverSlots = remotePlayers
            .Where(r => r.IsSystem == true && r.SystemSlot is 1 or 2)
            .ToDictionary(r => r.SystemSlot!.Value, r => r.Id);
        var replacedSeeds = local.Values.Where(p => p.IsLocalSeed && p.SystemSlot is int slot && serverSlots.ContainsKey(slot)).ToList();
        foreach (var seed in replacedSeeds.Where(s => !remoteIds.Contains(s.Id)))
        {
            db.PlayerSet.Remove(seed);
            local.Remove(seed.Id);
        }

        foreach (var remote in remotePlayers)
        {
            if (!local.TryGetValue(remote.Id, out var player))
            {
                player = new Player { Id = remote.Id };
                db.PlayerSet.Add(player);
                local[remote.Id] = player;
            }

            player.Nickname = remote.Nickname ?? "";
            player.Name = remote.Name ?? "";
            player.ShortcutNumber = remote.ShortcutNumber;
            player.AvatarId = remote.AvatarId ?? remote.Id % AvatarGenerator.Count;
            player.UpdatedAt = remote.UpdatedAt;
            player.IsSystem = remote.IsSystem == true;
            player.SystemSlot = remote.IsSystem == true ? remote.SystemSlot : null;

            player.PhotoPath = photoPaths.GetValueOrDefault(remote.Id, player.PhotoPath);
        }

        var gone = local.Values.Where(p => (prune ? !remoteIds.Contains(p.Id) : deletedIds.Contains(p.Id)) && !p.IsLocalSeed).ToList();
        foreach (var player in gone)
        {
            DeletePhoto(player.PhotoPath);
        }

        db.PlayerSet.RemoveRange(gone);

        var teamOfPlayer = new Dictionary<int, int>();
        foreach (var team in remoteTeams)
        {
            foreach (var member in team.Players)
            {
                teamOfPlayer.TryAdd(member.Id, team.Id);
            }
        }

        if (prune)
        {
            foreach (var player in local.Values.Where(p => !p.IsLocalSeed && remoteIds.Contains(p.Id)))
            {
                player.TeamId = teamOfPlayer.TryGetValue(player.Id, out var teamId) ? teamId : null;
            }
        }
        else
        {
            foreach (var team in remoteTeams)
            {
                var memberIds = team.Players.Select(m => m.Id).ToHashSet();
                foreach (var player in local.Values.Where(p => !p.IsLocalSeed && p.TeamId == team.Id && !memberIds.Contains(p.Id)))
                {
                    player.TeamId = null;
                }

                foreach (var player in local.Values.Where(p => !p.IsLocalSeed && memberIds.Contains(p.Id)))
                {
                    player.TeamId = team.Id;
                }
            }
        }

        await db.SaveChangesAsync(ct);

        var moves = replacedSeeds.Where(s => s.Id != serverSlots[s.SystemSlot!.Value]).ToDictionary(s => s.Id, s => serverSlots[s.SystemSlot!.Value]);
        if (moves.Count > 0)
        {
            var cases = string.Join(" ", moves.Select(m => $"WHEN {m.Key} THEN {m.Value}"));
            foreach (var (table, column, filter) in new[]
                     {
                         ("scoreboard_state", "Player1Id", ""), ("scoreboard_state", "Player2Id", ""),
                         ("match_result", "Player1Id", " WHERE SyncedAPI = 0"), ("match_result", "Player2Id", " WHERE SyncedAPI = 0")
                     })
            {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                await db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET {column} = CASE {column} {cases} ELSE {column} END {filter}", ct);
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
            }
        }

        if (gone.Count > 0)
        {
            var goneIds = gone.Select(p => p.Id).ToList();
            var default1 = serverSlots.GetValueOrDefault(1, 1);
            var default2 = serverSlots.GetValueOrDefault(2, 2);
            await db.ScoreboardStateSet.Where(s => goneIds.Contains(s.Player1Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player1Id, default1), ct);
            await db.ScoreboardStateSet.Where(s => goneIds.Contains(s.Player2Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player2Id, default2), ct);
        }
    }

    private async Task<Dictionary<int, string?>> DownloadPhotosAsync(HttpClient http, List<RemotePlayer> remotePlayers, CancellationToken ct)
    {
        var photoPaths = new Dictionary<int, string?>();
        if (remotePlayers.Count == 0)
        {
            return photoPaths;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var local = await db.PlayerSet.AsNoTracking().Select(p => new { p.Id, p.UpdatedAt, p.PhotoPath }).ToDictionaryAsync(p => p.Id, ct);
        foreach (var remote in remotePlayers)
        {
            local.TryGetValue(remote.Id, out var existing);
            var changed = existing is null || existing.UpdatedAt != remote.UpdatedAt;
            photoPaths[remote.Id] = await SyncPhotoAsync(http, remote, existing?.PhotoPath, changed, ct);
        }

        return photoPaths;
    }

    private async Task<string?> SyncPhotoAsync(HttpClient http, RemotePlayer remote, string? currentPath, bool changed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(remote.PhotoPath))
        {
            DeletePhoto(currentPath);
            return null;
        }

        var extension = Path.GetExtension(remote.PhotoPath);
        var relativePath = $"{_photosFolder}/{remote.Id}{(extension.Length is > 1 and <= 5 ? extension : ".jpg")}";
        var fullPath = Path.Combine(env.WebRootPath, _photosFolder, Path.GetFileName(relativePath));
        if (!changed && currentPath == relativePath && File.Exists(fullPath))
        {
            return currentPath;
        }

        try
        {
            var url = new Uri(new Uri(http.BaseAddress!, "/"), remote.PhotoPath);
            var bytes = await http.GetByteArrayAsync(url, ct);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);
            if (currentPath is not null && currentPath != relativePath)
            {
                DeletePhoto(currentPath);
            }

            return relativePath;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            LogPhotoFailed(remote.Id, ex);
            return File.Exists(Path.Combine(env.WebRootPath, currentPath ?? "-")) ? currentPath : null;
        }
    }

    private void DeletePhoto(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return;
        }

        try
        {
            var fullPath = Path.Combine(env.WebRootPath, relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogPhotoDeleteFailed(relativePath, ex);
        }
    }

    private record RemoteOrganization(string Name, string Language, string? TimeZone = null);

    private record RemoteChangeGroup(string EntityType, string State, List<JsonElement>? Items, List<int>? Ids);

    private record RemoteChanges(bool Full, List<RemoteChangeGroup> Changes)
    {
        public List<T> ItemsOf<T>(string entityType) =>
            Changes
                .Where(g => g.EntityType == entityType && g.State == "changed" && g.Items is not null)
                .SelectMany(g => g.Items!)
                .Select(item => item.Deserialize<T>(_jsonOptions)!)
                .ToList();

        public HashSet<int> DeletedIdsOf(string entityType) =>
            Changes
                .Where(g => g.EntityType == entityType && g.State == "deleted" && g.Ids is not null)
                .SelectMany(g => g.Ids!)
                .ToHashSet();
    }

    private record RemoteClub(int Id, string Name, string ShortName, string? City, string? PrimaryColor);

    private record RemotePlayer(
        int Id, string? Nickname, string? Name, string? PhotoPath, int? AvatarId,
        DateTimeOffset? UpdatedAt, int? ShortcutNumber,
        bool? IsSystem = null, int? SystemSlot = null);

    private record RemoteTeam(int Id, int ClubId, string Name, DateTimeOffset? UpdatedAt, List<RemoteTeamPlayer> Players, int? AvatarId = null);

    private record RemoteTeamPlayer(int Id, string? Nickname, string? Name);

    [LoggerMessage(Level = LogLevel.Information, Message = "RemotePull is disabled (RemoteSync:Enabled=false) - skipping remote data pull.")]
    private partial void LogPullDisabled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemotePull is enabled but RemoteSync:BaseUrl is empty - skipping remote data pull.")]
    private partial void LogPullNoBaseUrl();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not confirm the received changes to the server: {Error}")]
    private partial void LogAckFailed(string error, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No changes on the server.")]
    private partial void LogNoChanges();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemotePull is enabled but RemoteSync:ClientId is empty - skipping remote data pull.")]
    private partial void LogPullNoClientId();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote pull tick failed; will retry next interval.")]
    private partial void LogPullTickFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "RemotePull could not start; remote data pull is disabled for this run.")]
    private partial void LogPullStartFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND GET clubs, players, teams")]
    private partial void LogSendGet();

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED {Clubs} club(s), {Teams} team(s), {Players} player(s) from remote")]
    private partial void LogReceived(int clubs, int teams, int players);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not download the photo of player {PlayerId}; keeping the current one.")]
    private partial void LogPhotoFailed(int playerId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not delete photo {Path}.")]
    private partial void LogPhotoDeleteFailed(string path, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Receiving operation failed for player. {Message}")]
    private partial void LogReceivedFailed(string Message, Exception ex);
}
