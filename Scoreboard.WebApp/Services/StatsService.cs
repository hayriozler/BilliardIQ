using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Services;

public record PlayerMatchEntry(
    int MatchId,
    DateTimeOffset PlayedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    int? TableNo,
    string OpponentName,
    int PlayerScore,
    int OpponentScore,
    int Inning,
    int HighRun,
    double Average,
    bool Won,
    int PlayerSlot,
    int BucketMinutes,
    IReadOnlyList<int> PaceBuckets);

public record StatSummary(
    int Matches,
    int Wins,
    int Losses,
    double WinPercent,
    double AverageInnings,
    double AveragePerInning,
    double BestAverage,
    int BestHighRun);

public record PlayerStatRow(Player Player, StatSummary Summary);

public record PlayerStatDetail(Player Player, StatSummary Summary, IReadOnlyList<PlayerMatchEntry> Matches);

public record TeamStatRow(Team Team, int MemberCount, StatSummary Summary);

public record TeamStatDetail(Team Team, StatSummary Summary, IReadOnlyList<PlayerStatRow> Members, IReadOnlyList<(Player Player, PlayerMatchEntry Match)> RecentMatches);

public class StatsService(DataContext db)
{
    public async Task<List<PlayerStatRow>> PlayersAsync(int organizationId, DateTimeOffset? since = null)
    {
        var players = await RosterPlayersAsync(organizationId);
        var entries = await EntriesByPlayerAsync(organizationId, since, includeBuckets: false);
        return players
            .Select(p => new PlayerStatRow(p, Summarize(entries.GetValueOrDefault(p.Id) ?? [])))
            .OrderByDescending(r => r.Summary.Wins).ThenByDescending(r => r.Summary.Matches)
            .ThenBy(r => r.Player.DisplayName)
            .ToList();
    }

    public async Task<PlayerStatDetail?> PlayerAsync(int organizationId, int playerId, DateTimeOffset? since = null)
    {
        var player = await db.PlayerSet.AsNoTracking()
            .Include(p => p.Association)
            .FirstOrDefaultAsync(p => p.Id == playerId && p.CreatedInOrganizationId == organizationId && p.DeletedAt == null);
        if (player is null)
        {
            return null;
        }

        var entries = (await EntriesByPlayerAsync(organizationId, since, includeBuckets: true, onlyPlayerId: playerId))
            .GetValueOrDefault(playerId) ?? [];
        return new PlayerStatDetail(player, Summarize(entries), entries);
    }

    public async Task<List<TeamStatRow>> TeamsAsync(int organizationId, DateTimeOffset? since = null)
    {
        var teams = await TeamsWithMembersAsync(organizationId);
        var entries = await EntriesByPlayerAsync(organizationId, since, includeBuckets: false);
        return teams
            .Select(t =>
            {
                var memberIds = t.Members.Select(m => m.PlayerId).Distinct().ToList();
                var teamEntries = memberIds.SelectMany(id => entries.GetValueOrDefault(id) ?? []).ToList();
                return new TeamStatRow(t, memberIds.Count, Summarize(teamEntries));
            })
            .OrderByDescending(r => r.Summary.Wins).ThenByDescending(r => r.Summary.Matches).ThenBy(r => r.Team.Name)
            .ToList();
    }

    public async Task<TeamStatDetail?> TeamAsync(int organizationId, int teamId, DateTimeOffset? since = null)
    {
        var team = (await TeamsWithMembersAsync(organizationId)).FirstOrDefault(t => t.Id == teamId);
        if (team is null)
        {
            return null;
        }

        var entries = await EntriesByPlayerAsync(organizationId, since, includeBuckets: false);
        var members = team.Members.Select(m => m.Player).DistinctBy(p => p.Id).ToList();
        var memberRows = members
            .Select(p => new PlayerStatRow(p, Summarize(entries.GetValueOrDefault(p.Id) ?? [])))
            .OrderByDescending(r => r.Summary.Wins).ThenByDescending(r => r.Summary.Matches).ThenBy(r => r.Player.DisplayName)
            .ToList();
        var all = members.SelectMany(p => entries.GetValueOrDefault(p.Id) ?? []).ToList();
        var recent = members
            .SelectMany(p => (entries.GetValueOrDefault(p.Id) ?? []).Select(e => (Player: p, Match: e)))
            .OrderByDescending(x => x.Match.PlayedAt)
            .Take(20)
            .ToList();
        return new TeamStatDetail(team, Summarize(all), memberRows, recent);
    }

    private Task<List<Player>> RosterPlayersAsync(int organizationId) =>
        db.PlayerSet.AsNoTracking()
            .Where(p => p.CreatedInOrganizationId == organizationId && p.DeletedAt == null && !p.IsSystem)
            .ToListAsync();

    private Task<List<Team>> TeamsWithMembersAsync(int organizationId) =>
        db.TeamSet.AsNoTracking()
            .Include(t => t.Club)
            .Include(t => t.Members.Where(m => m.LeftAt == null)).ThenInclude(m => m.Player)
            .Where(t => t.Club.OrganizationId == organizationId && t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .ToListAsync();

    private async Task<Dictionary<int, List<PlayerMatchEntry>>> EntriesByPlayerAsync(
        int organizationId, DateTimeOffset? since, bool includeBuckets, int? onlyPlayerId = null)
    {
        var query = db.MatchStatSet.AsNoTracking().Where(s => s.OrganizationId == organizationId);
        if (since is not null)
        {
            query = query.Where(s => s.PlayedAt >= since);
        }

        if (onlyPlayerId is int only)
        {
            query = query.Where(s => s.Player1ExternalId == only || s.Player2ExternalId == only);
        }
        else
        {
            query = query.Where(s => s.Player1ExternalId != null || s.Player2ExternalId != null);
        }

        if (includeBuckets)
        {
            query = query.Include(s => s.Buckets);
        }

        var stats = await query.OrderBy(s => s.PlayedAt).ToListAsync();
        var result = new Dictionary<int, List<PlayerMatchEntry>>();
        foreach (var s in stats)
        {
            AddEntry(result, s, slot: 1);
            AddEntry(result, s, slot: 2);
        }

        return result;
    }

    private static void AddEntry(Dictionary<int, List<PlayerMatchEntry>> result, MatchStat s, int slot)
    {
        var playerId = slot == 1 ? s.Player1ExternalId : s.Player2ExternalId;
        if (playerId is not int id)
        {
            return;
        }

        var first = slot == 1;
        var buckets = Array.Empty<int>();
        if (s.Buckets.Count > 0)
        {
            var mine = s.Buckets.Where(b => b.PlayerSlot == slot).ToList();
            if (mine.Count > 0)
            {
                buckets = new int[mine.Max(b => b.BucketIndex) + 1];
                foreach (var b in mine)
                {
                    buckets[b.BucketIndex] += b.TotalPoints;
                }
            }
        }

        if (!result.TryGetValue(id, out var list))
        {
            result[id] = list = [];
        }

        list.Add(new PlayerMatchEntry(
            s.Id, s.PlayedAt, s.StartedAt, s.EndedAt, s.TableNo,
            first ? s.Player2Name : s.Player1Name,
            first ? s.Player1Score : s.Player2Score,
            first ? s.Player2Score : s.Player1Score,
            s.Inning,
            first ? s.Player1HighRun : s.Player2HighRun,
            first ? s.Player1Avg : s.Player2Avg,
            s.Winner == slot,
            slot, s.BucketMinutes, buckets));
    }

    public static StatSummary Summarize(IReadOnlyCollection<PlayerMatchEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new StatSummary(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var wins = entries.Count(e => e.Won);
        return new StatSummary(
            entries.Count,
            wins,
            entries.Count - wins,
            wins * 100.0 / entries.Count,
            entries.Average(e => (double)e.Inning),
            entries.Average(e => e.Average),
            entries.Max(e => e.Average),
            entries.Max(e => e.HighRun));
    }
}
