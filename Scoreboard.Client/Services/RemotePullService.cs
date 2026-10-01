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
    ILogger<RemotePullService> logger) : BackgroundService
{
    private const string PhotosFolder = "PlayerSet";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RemoteSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
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

            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            var http = httpClientFactory.CreateClient(nameof(RemotePullService));
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);
            if (_options.TableNo > 0)
            {
                http.DefaultRequestHeaders.Add("X-Table-No", _options.TableNo.ToString());
            }

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await PullOnceAsync(http, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
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

    private async Task PullOnceAsync(HttpClient http, CancellationToken ct)
    {
        if (systemPower.IsShuttingDown)
        {
            return;
        }

        LogSendGet();
        RemoteOrganization? organization = null;
        List<RemoteClub> clubs = [];
        List<RemotePlayer> players = [];
        List<RemoteTeam> teams = [];
        try
        {
            organization = await http.GetFromJsonAsync<RemoteOrganization>("scoreboard/organization", _jsonOptions, ct);
            clubs = await http.GetFromJsonAsync<List<RemoteClub>>("scoreboard/clubs", _jsonOptions, ct) ?? [];
            players = await http.GetFromJsonAsync<List<RemotePlayer>>("scoreboard/players", _jsonOptions, ct) ?? [];
            teams = await http.GetFromJsonAsync<List<RemoteTeam>>("scoreboard/teams", _jsonOptions, ct) ?? [];
            LogReceived(clubs.Count, teams.Count, players.Count);
        }
        catch (HttpRequestException e)
        {
            LogReceivedFailed(e.Message, e);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var languageChanged = await ApplyServerLanguageAsync(db, organization?.Language, ct);
        languageChanged |= await ApplyOrganizationNameAsync(db, organization, ct);
        await MirrorClubsAsync(db, clubs, ct);
        await MirrorTeamsAsync(db, teams, ct);
        await MirrorPlayersAsync(db, http, players, teams, ct);

        await transaction.CommitAsync(ct);

        if (languageChanged)
        {
            languageSync.Notify();
        }
    }

    private static async Task<bool> ApplyOrganizationNameAsync(DataContext db, RemoteOrganization? organization, CancellationToken ct)
    {
        if (organization is null)
        {
            return false;
        }

        var name = organization.Name?.Trim() ?? "";
        var setting = await db.SettingsSet.FirstOrDefaultAsync(s => s.Id == "OrgName", ct);
        if ((setting?.Value ?? "") == name)
        {
            return false;
        }

        if (setting is null)
        {
            db.SettingsSet.Add(new Setting { Id = "OrgName", Value = name });
        }
        else
        {
            setting.Value = name;
        }

        await db.SaveChangesAsync(ct);
        return true;
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
        return true;
    }

    private static async Task MirrorClubsAsync(DataContext db, List<RemoteClub> remoteClubs, CancellationToken ct)
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
        db.ClubSet.RemoveRange(local.Values.Where(c => !remoteIds.Contains(c.Id)));
        await db.SaveChangesAsync(ct);
    }

    private static async Task MirrorTeamsAsync(DataContext db, List<RemoteTeam> remoteTeams, CancellationToken ct)
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
        db.TeamSet.RemoveRange(local.Values.Where(t => !remoteIds.Contains(t.Id)));
        await db.SaveChangesAsync(ct);
    }

    private async Task MirrorPlayersAsync(
        DataContext db, HttpClient http, List<RemotePlayer> remotePlayers, List<RemoteTeam> remoteTeams, CancellationToken ct)
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

            var photoChanged = player.UpdatedAt != remote.UpdatedAt;
            player.Nickname = remote.Nickname ?? "";
            player.Name = remote.Name ?? "";
            player.ShortcutNumber = remote.ShortcutNumber;
            player.AvatarId = remote.AvatarId ?? remote.Id % AvatarGenerator.Count;
            player.UpdatedAt = remote.UpdatedAt;
            player.IsSystem = remote.IsSystem == true;
            player.SystemSlot = remote.IsSystem == true ? remote.SystemSlot : null;

            player.PhotoPath = await SyncPhotoAsync(http, remote, player.PhotoPath, photoChanged, ct);
        }

        var gone = local.Values.Where(p => !remoteIds.Contains(p.Id) && !p.IsLocalSeed).ToList();
        foreach (var player in gone)
        {
            DeletePhoto(player.PhotoPath);
        }

        db.PlayerSet.RemoveRange(gone);

        // Team membership. A player belongs to one team locally; when the server lists several the first wins.
        var teamOfPlayer = new Dictionary<int, int>();
        foreach (var team in remoteTeams)
        {
            foreach (var member in team.Players)
            {
                teamOfPlayer.TryAdd(member.Id, team.Id);
            }
        }

        foreach (var player in local.Values.Where(p => !p.IsLocalSeed && remoteIds.Contains(p.Id)))
        {
            player.TeamId = teamOfPlayer.TryGetValue(player.Id, out var teamId) ? teamId : null;
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
#pragma warning disable EF1002 // identifiers are literals and the CASE arms are integer ids from our own tables
                await db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET {column} = CASE {column} {cases} ELSE {column} END{filter}", ct);
#pragma warning restore EF1002
            }
        }

        // A removed player on the board falls back to Player 1 / Player 2.
        if (gone.Count > 0)
        {
            var goneIds = gone.Select(p => p.Id).ToList();
            var default1 = serverSlots.GetValueOrDefault(1, 1);
            var default2 = serverSlots.GetValueOrDefault(2, 2);
            await db.ScoreboardStateSet.Where(s => goneIds.Contains(s.Player1Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player1Id, default1), ct);
            await db.ScoreboardStateSet.Where(s => goneIds.Contains(s.Player2Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player2Id, default2), ct);
        }
    }

    /// <summary>Downloads the player's photo when it is new or changed. Best effort: failures keep what is there.</summary>
    private async Task<string?> SyncPhotoAsync(HttpClient http, RemotePlayer remote, string? currentPath, bool changed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(remote.PhotoPath))
        {
            DeletePhoto(currentPath);
            return null;
        }

        var extension = Path.GetExtension(remote.PhotoPath);
        var relativePath = $"{PhotosFolder}/{remote.Id}{(extension.Length is > 1 and <= 5 ? extension : ".jpg")}";
        var fullPath = Path.Combine(env.WebRootPath, PhotosFolder, Path.GetFileName(relativePath));
        if (!changed && currentPath == relativePath && File.Exists(fullPath))
        {
            return currentPath;
        }

        try
        {
            // The server serves photos from its site root, next to (not under) the /api/ base address.
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
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
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

    private record RemoteOrganization(string Name, string Language);

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
