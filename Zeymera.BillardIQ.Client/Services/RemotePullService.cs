using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Zeymera.BillardIQ.Client.Models;

namespace Zeymera.BillardIQ.Client.Services;

public partial class RemotePullService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<RemoteSyncOptions> options,
    SystemPowerService systemPower,
    ILogger<RemotePullService> logger) : BackgroundService
{
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

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await PullPlayersAsync(db, http, ct);
        await PullTeamsAsync(db, http, ct);
    }
    private async Task PullTeamsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        LogSendGetTeams();
        var remoteTeams = await http.GetFromJsonAsync<List<RemoteTeam>>("teams", _jsonOptions, ct);
        LogReceivedTeams(remoteTeams?.Count ?? 0);
        if (remoteTeams is null)
        {
            return;
        }

        foreach (var remote in remoteTeams)
        {
            var team = await db.TeamSet.FirstOrDefaultAsync(t => t.RemoteId == remote.Id, ct);
            if (team is null)
            {
                team = new Team { RemoteId = remote.Id, SyncedAPI = true };
                db.TeamSet.Add(team);
            }

            team.Name = remote.Name;
            await db.SaveChangesAsync(ct);

            var memberRemoteIds = remote.Players.Select(p => p.Id).ToHashSet();
            var members = await db.PlayerSet.Where(p => p.RemoteId != null && memberRemoteIds.Contains(p.RemoteId!.Value)).ToListAsync(ct);
            foreach (var member in members)
            {
                member.TeamId = team.Id;
            }

            var formerMembers = await db.PlayerSet.Where(p => p.TeamId == team.Id && !memberRemoteIds.Contains(p.RemoteId ?? -1)).ToListAsync(ct);
            foreach (var former in formerMembers)
            {
                former.TeamId = null;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PullPlayersAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        LogSendGetPlayers();
        var remotePlayers = await http.GetFromJsonAsync<List<RemotePlayer>>("players", _jsonOptions, ct);
        LogReceivedPlayers(remotePlayers?.Count ?? 0);
        if (remotePlayers is null)
        {
            return;
        }

        foreach (var remote in remotePlayers)
        {
            await UpsertPulledPlayerAsync(db, remote, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<Player> UpsertPulledPlayerAsync(DataContext db, RemotePlayer remote, CancellationToken ct)
    {
        var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.RemoteId == remote.Id, ct);

        if (player is null && remote.ClientId == _options.ClientId)
        {
            player = await db.PlayerSet.FirstOrDefaultAsync(p => p.Id == remote.ExternalId && p.RemoteId == null, ct);
        }

        if (player is null)
        {
            player = new Player { SyncedAPI = true };
            db.PlayerSet.Add(player);
        }

        player.RemoteId = remote.Id;

        if (player.SyncedAPI)
        {
            player.Nickname = remote.Nickname;
            player.Name = remote.Name;
            player.AvatarId = remote.AvatarId;
        }

        return player;
    }
    private record RemotePlayer(int Id, string ClientId, int ExternalId, string Nickname, string Name, int? AvatarId);
    private record RemoteTeam(int Id, string ClientId, int ExternalId, string Name, List<RemoteTeamPlayer> Players);
    private record RemoteTeamPlayer(int Id, int PlayerId, string Nickname, string Name);

    [LoggerMessage(Level = LogLevel.Information, Message = "RemotePull is disabled (RemoteSync:Enabled=false) - skipping remote data pull.")]
    private partial void LogPullDisabled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemotePull is enabled but RemoteSync:BaseUrl is empty - skipping remote data pull.")]
    private partial void LogPullNoBaseUrl();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote pull tick failed; will retry next interval.")]
    private partial void LogPullTickFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "RemotePull could not start; remote data pull is disabled for this run.")]
    private partial void LogPullStartFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND GET teams request")]
    private partial void LogSendGetTeams();

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED {Count} team(s) from remote")]
    private partial void LogReceivedTeams(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND GET players request")]
    private partial void LogSendGetPlayers();

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED {Count} player(s) from remote")]
    private partial void LogReceivedPlayers(int count);
}
