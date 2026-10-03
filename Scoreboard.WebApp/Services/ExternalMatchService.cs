using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public record ExternalMatchInput(DateOnly PlayedOn, string OpponentName, string? Venue, int Score, int OpponentScore, int Innings, int HighRun, ExternalOutcome? Outcome);

public record ExternalMatchDto(
    int Id, int PlayerId, string PlayerName, DateOnly PlayedOn, string OpponentName, string? Venue,
    int Score, int OpponentScore, int Innings, int HighRun, double Average, ExternalOutcome Outcome);

public record ExternalSummaryDto(int Matches, int Wins, int Draws, int Losses, double WinPercent, double AveragePerInning, double BestAverage, int BestHighRun);

public class ExternalMatchService(DataContext db)
{
    public async Task<List<ExternalMatchDto>> ListAsync(int? playerId = null)
    {
        var query = db.ExternalMatchSet.AsNoTracking().Include(m => m.Player).AsQueryable();
        if (playerId is { } id)
        {
            query = query.Where(m => m.PlayerId == id);
        }

        var rows = await query.OrderByDescending(m => m.PlayedOn).ThenByDescending(m => m.Id).ToListAsync();
        return [.. rows.Select(ToDto)];
    }

    public async Task<ExternalSummaryDto> SummaryAsync(int playerId)
    {
        var rows = await db.ExternalMatchSet.AsNoTracking().Where(m => m.PlayerId == playerId).ToListAsync();
        if (rows.Count == 0)
        {
            return new ExternalSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var wins = rows.Count(m => m.Outcome == ExternalOutcome.Won);
        var totalScore = rows.Sum(m => m.Score);
        var totalInnings = rows.Sum(m => m.Innings);
        return new ExternalSummaryDto(
            rows.Count,
            wins,
            rows.Count(m => m.Outcome == ExternalOutcome.Draw),
            rows.Count(m => m.Outcome == ExternalOutcome.Lost),
            Math.Round(100.0 * wins / rows.Count, 1),
            Average(totalScore, totalInnings),
            rows.Max(m => Average(m.Score, m.Innings)),
            rows.Max(m => m.HighRun));
    }

    public async Task<ExternalMatchDto> CreateAsync(int playerId, ExternalMatchInput input)
    {
        Validate(input);
        var match = new ExternalMatch { PlayerId = playerId, OrganizationId = db.CurrentOrganizationId };
        Apply(match, input);
        db.ExternalMatchSet.Add(match);
        await db.SaveChangesAsync();
        return await ReloadAsync(match.Id);
    }

    public async Task<ExternalMatchDto?> UpdateAsync(int playerId, int id, ExternalMatchInput input)
    {
        Validate(input);
        var match = await db.ExternalMatchSet.FirstOrDefaultAsync(m => m.Id == id && m.PlayerId == playerId);
        if (match is null)
        {
            return null;
        }

        Apply(match, input);
        await db.SaveChangesAsync();
        return await ReloadAsync(match.Id);
    }

    public async Task<bool> DeleteAsync(int playerId, int id)
    {
        var match = await db.ExternalMatchSet.FirstOrDefaultAsync(m => m.Id == id && m.PlayerId == playerId);
        if (match is null)
        {
            return false;
        }

        db.ExternalMatchSet.Remove(match);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<ExternalMatchDto> ReloadAsync(int id) =>
        ToDto(await db.ExternalMatchSet.AsNoTracking().Include(m => m.Player).FirstAsync(m => m.Id == id));

    private static void Validate(ExternalMatchInput input)
    {
        if (string.IsNullOrWhiteSpace(input.OpponentName) || input.OpponentName.Trim().Length > 100)
        {
            throw new ArgumentException("Rakip adı gerekli (en fazla 100 karakter).");
        }

        if (input.Venue is { Length: > 100 })
        {
            throw new ArgumentException("Yer en fazla 100 karakter olabilir.");
        }

        if (input.Score < 0 || input.OpponentScore < 0 || input.HighRun < 0)
        {
            throw new ArgumentException("Sayılar negatif olamaz.");
        }

        if (input.Innings < 1)
        {
            throw new ArgumentException("El sayısı en az 1 olmalı.");
        }

        if (input.HighRun > input.Score)
        {
            throw new ArgumentException("En yüksek seri toplam sayıdan büyük olamaz.");
        }

        if (input.PlayedOn > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
        {
            throw new ArgumentException("Maç tarihi gelecekte olamaz.");
        }
    }

    private static void Apply(ExternalMatch match, ExternalMatchInput input)
    {
        match.PlayedOn = input.PlayedOn;
        match.OpponentName = input.OpponentName.Trim();
        match.Venue = string.IsNullOrWhiteSpace(input.Venue) ? null : input.Venue.Trim();
        match.Score = input.Score;
        match.OpponentScore = input.OpponentScore;
        match.Innings = input.Innings;
        match.HighRun = input.HighRun;
        match.Outcome = input.Outcome ?? (input.Score > input.OpponentScore ? ExternalOutcome.Won
            : input.Score < input.OpponentScore ? ExternalOutcome.Lost : ExternalOutcome.Draw);
    }

    private static ExternalMatchDto ToDto(ExternalMatch m) => new(
        m.Id, m.PlayerId, m.Player.DisplayName, m.PlayedOn, m.OpponentName, m.Venue,
        m.Score, m.OpponentScore, m.Innings, m.HighRun, Average(m.Score, m.Innings), m.Outcome);

    private static double Average(int score, int innings) => innings == 0 ? 0 : Math.Round((double)score / innings, 3);
}
