using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record RankingRow(
    int Rank, int PlayerId, string Name, int? AvatarId, string? PhotoUrl,
    int Matches, int Wins, int Losses, double WinPercent, double AveragePerInning, double BestAverage, int BestHighRun);

public record StatsOverview(
    int ActivePlayers, int PlayerMatches, double AveragePerInning,
    RankingRow? TopAverage, RankingRow? TopHighRun, RankingRow? MostWins, RankingRow? MostMatches);

public static class MobileStatsEndpoints
{
    private const int MinMatchesForAverage = 3;

    public static IEndpointRouteBuilder MapMobileStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/stats")
            .WithTags("MobileStats")
            .RequireAuthorization(JwtSettings.MobilePolicy);

        group.MapGet("/ranking", async (StatsService stats, string? period, DateOnly? from, DateOnly? to, string? sort, int? minMatches, int? limit) =>
        {
            var rows = await ActiveRowsAsync(stats, period, from, to);
            var min = Math.Max(1, minMatches ?? 1);
            var ordered = Sort(rows.Where(r => r.Matches >= min), sort).Take(Math.Clamp(limit ?? 100, 1, 500)).ToList();
            return ordered.Select((r, i) => r with { Rank = i + 1 });
        });

        group.MapGet("/overview", async (StatsService stats, string? period, DateOnly? from, DateOnly? to) =>
        {
            var rows = await ActiveRowsAsync(stats, period, from, to);
            if (rows.Count == 0)
            {
                return new StatsOverview(0, 0, 0, null, null, null, null);
            }

            var matches = rows.Sum(r => r.Matches);
            var weighted = matches == 0 ? 0 : Math.Round(rows.Sum(r => r.AveragePerInning * r.Matches) / matches, 3);
            var qualified = rows.Where(r => r.Matches >= MinMatchesForAverage).ToList();
            return new StatsOverview(
                rows.Count,
                matches,
                weighted,
                Sort(qualified, "average").FirstOrDefault(),
                Sort(rows, "highrun").FirstOrDefault(),
                Sort(rows, "wins").FirstOrDefault(),
                Sort(rows, "matches").FirstOrDefault());
        });

        return app;
    }

    private static async Task<List<RankingRow>> ActiveRowsAsync(StatsService stats, string? period, DateOnly? from, DateOnly? to)
    {
        var range = new StatsPeriod(period ?? StatsPeriod.Default.Kind, from, to);
        var rows = await stats.PlayersAsync(range.Since, range.Until);
        return [.. rows
            .Where(r => !r.Player.IsSystem && r.Summary.Matches > 0)
            .Select(r => new RankingRow(
                0, r.Player.Id, r.Player.DisplayName, r.Player.AvatarId, r.Player.PhotoUrl,
                r.Summary.Matches, r.Summary.Wins, r.Summary.Losses, r.Summary.WinPercent,
                r.Summary.AveragePerInning, r.Summary.BestAverage, r.Summary.BestHighRun))];
    }

    private static IEnumerable<RankingRow> Sort(IEnumerable<RankingRow> rows, string? sort) => (sort ?? "average").ToLowerInvariant() switch
    {
        "wins" => rows.OrderByDescending(r => r.Wins).ThenByDescending(r => r.WinPercent).ThenBy(r => r.Name),
        "highrun" => rows.OrderByDescending(r => r.BestHighRun).ThenByDescending(r => r.AveragePerInning).ThenBy(r => r.Name),
        "matches" => rows.OrderByDescending(r => r.Matches).ThenByDescending(r => r.Wins).ThenBy(r => r.Name),
        "winpercent" => rows.OrderByDescending(r => r.WinPercent).ThenByDescending(r => r.Matches).ThenBy(r => r.Name),
        _ => rows.OrderByDescending(r => r.AveragePerInning).ThenByDescending(r => r.Matches).ThenBy(r => r.Name)
    };
}
