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
        var clubs = await http.GetFromJsonAsync<List<RemoteClub>>("clubs", _jsonOptions, ct);
        var players = await http.GetFromJsonAsync<List<RemotePlayer>>("players", _jsonOptions, ct);
        var teams = await http.GetFromJsonAsync<List<RemoteTeam>>("teams", _jsonOptions, ct);
        if (clubs is null || players is null || teams is null)
        {
            return;
        }

        LogReceived(clubs.Count, teams.Count, players.Count);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var clubIds = await MirrorClubsAsync(db, clubs, ct);
        var playerIds = await MirrorPlayersAsync(db, http, players, ct);
        await MirrorTeamsAsync(db, teams, clubIds, playerIds, ct);

        await transaction.CommitAsync(ct);
    }
    private static async Task<Dictionary<int, int>> MirrorClubsAsync(DataContext db, List<RemoteClub> remoteClubs, CancellationToken ct)
    {
        var local = await db.ClubSet.ToListAsync(ct);
        var byRemoteId = local.Where(c => c.RemoteId != null).ToDictionary(c => c.RemoteId!.Value);

        foreach (var remote in remoteClubs)
        {
            if (!byRemoteId.TryGetValue(remote.Id, out var club))
            {
                club = new Club { RemoteId = remote.Id };
                db.ClubSet.Add(club);
                byRemoteId[remote.Id] = club;
            }

            club.Name = remote.Name;
            club.ShortName = remote.ShortName;
            club.City = remote.City;
            club.PrimaryColor = remote.PrimaryColor;
        }

        var remoteIds = remoteClubs.Select(c => c.Id).ToHashSet();
        var gone = local.Where(c => c.RemoteId is not int id || !remoteIds.Contains(id)).ToList();
        if (gone.Count > 0)
        {
            var goneIds = gone.Select(c => c.Id).ToList();
            foreach (var team in await db.TeamSet.Where(t => t.ClubId != null && goneIds.Contains(t.ClubId.Value)).ToListAsync(ct))
            {
                team.ClubId = null;
            }

            db.ClubSet.RemoveRange(gone);
        }

        await db.SaveChangesAsync(ct);
        return byRemoteId.Where(kv => remoteIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value.Id);
    }

    /// <returns>Server player id → local player id.</returns>
    private async Task<Dictionary<int, int>> MirrorPlayersAsync(DataContext db, HttpClient http, List<RemotePlayer> remotePlayers, CancellationToken ct)
    {
        var local = await db.PlayerSet.ToListAsync(ct);
        var byRemoteId = local.Where(p => p.RemoteId != null).ToDictionary(p => p.RemoteId!.Value);

        foreach (var remote in remotePlayers)
        {
            if (!byRemoteId.TryGetValue(remote.Id, out var player))
            {
                player = remote.IsSystem == true && remote.SystemSlot is 1 or 2
                    ? local.FirstOrDefault(p => p.Id == remote.SystemSlot && p.RemoteId is null)
                    : null;
                if (player is not null)
                {
                    player.RemoteId = remote.Id;
                    byRemoteId[remote.Id] = player;
                }
            }

            if (player is null)
            {
                player = new Player { RemoteId = remote.Id };
                db.PlayerSet.Add(player);
                byRemoteId[remote.Id] = player;
            }

            var photoChanged = player.UpdatedAt != remote.UpdatedAt;
            player.Nickname = remote.Nickname ?? "";
            player.Name = remote.Name ?? "";
            player.ShortcutNumber = remote.ShortcutNumber;
            player.AvatarId = remote.AvatarId ?? remote.Id % AvatarGenerator.Count;
            player.Level = remote.Level;
            player.Country = remote.BaseCountry;
            player.City = remote.BaseCity;
            player.LicenseNo = remote.LicenseNo;
            player.LicenseValidUntil = remote.LicenseValidUntil;
            player.AssociationName = remote.AssociationName;
            player.UpdatedAt = remote.UpdatedAt;
            player.IsSystem = remote.IsSystem == true;
            player.SystemSlot = remote.IsSystem == true ? remote.SystemSlot : null;

            player.PhotoPath = await SyncPhotoAsync(http, remote, player.PhotoPath, photoChanged, ct);
        }

        var remoteIds = remotePlayers.Select(p => p.Id).ToHashSet();
        var gone = local.Where(p => !p.IsPlaceholder && !p.IsSystem && (p.RemoteId is not int id || !remoteIds.Contains(id))).ToList();
        foreach (var player in gone)
        {
            DeletePhoto(player.PhotoPath);
        }

        db.PlayerSet.RemoveRange(gone);
        await db.SaveChangesAsync(ct);

        foreach (var player in gone.Where(p => p.Id > 0))
        {
            await db.ScoreboardStateSet.Where(s => s.Player1Id == player.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player1Id, 1), ct);
            await db.ScoreboardStateSet.Where(s => s.Player2Id == player.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Player2Id, 2), ct);
        }

        return byRemoteId.Where(kv => remoteIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value.Id);
    }

    private async Task MirrorTeamsAsync(
        DataContext db, List<RemoteTeam> remoteTeams, Dictionary<int, int> clubIds, Dictionary<int, int> playerIds, CancellationToken ct)
    {
        var local = await db.TeamSet.ToListAsync(ct);
        var byRemoteId = local.Where(t => t.RemoteId != null).ToDictionary(t => t.RemoteId!.Value);

        foreach (var remote in remoteTeams)
        {
            if (!byRemoteId.TryGetValue(remote.Id, out var team))
            {
                team = new Team { RemoteId = remote.Id };
                db.TeamSet.Add(team);
                byRemoteId[remote.Id] = team;
            }

            team.Name = remote.Name;
            team.ClubId = clubIds.TryGetValue(remote.ClubId, out var clubId) ? clubId : null;
            team.UpdatedAt = remote.UpdatedAt;
        }

        var remoteIds = remoteTeams.Select(t => t.Id).ToHashSet();
        db.TeamSet.RemoveRange(local.Where(t => t.RemoteId is not int id || !remoteIds.Contains(id)));
        await db.SaveChangesAsync(ct);

        // Team membership. A player belongs to one team locally; when the server lists several the first wins.
        var teamOfPlayer = new Dictionary<int, int>();
        foreach (var remote in remoteTeams)
        {
            foreach (var member in remote.Players)
            {
                if (playerIds.TryGetValue(member.Id, out var localPlayerId))
                {
                    teamOfPlayer.TryAdd(localPlayerId, byRemoteId[remote.Id].Id);
                }
            }
        }

        foreach (var player in await db.PlayerSet.Where(p => p.Id > 2).ToListAsync(ct))
        {
            player.TeamId = teamOfPlayer.TryGetValue(player.Id, out var teamId) ? teamId : null;
        }

        await db.SaveChangesAsync(ct);
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

    private record RemoteClub(int Id, string Name, string ShortName, string? City, string? PrimaryColor);

    private record RemotePlayer(
        int Id, string? Nickname, string? Name, string? PhotoPath, int? AvatarId, int? Level,
        string? BaseCountry, string? BaseCity, DateTimeOffset? UpdatedAt, int? ShortcutNumber,
        string? LicenseNo, DateOnly? LicenseValidUntil, string? AssociationName,
        bool? IsSystem = null, int? SystemSlot = null);

    private record RemoteTeam(int Id, int ClubId, string Name, DateTimeOffset? UpdatedAt, List<RemoteTeamPlayer> Players);

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
}
