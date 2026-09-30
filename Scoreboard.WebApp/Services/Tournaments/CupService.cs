using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services.Tournaments;

/// <summary>Salon tournaments: setup, round rules, fixture generation, results and standings.</summary>
public class CupService(DataContext db)
{
    // ---------------------------------------------------------------- reading

    public Task<List<Cup>> ListAsync(int organizationId) =>
        db.CupSet
            .AsNoTracking()
            .Include(c => c.Participants)
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
            .ToListAsync();

    public Task<Cup?> GetAsync(int organizationId, int id) =>
        db.CupSet
            .AsNoTracking()
            .Include(c => c.Participants).ThenInclude(p => p.Player)
            .Include(c => c.RuleBlocks)
            .Include(c => c.Matches).ThenInclude(m => m.Table)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null);

    /// <summary>Players of the salon that can join a tournament (system players are placeholders and never can).</summary>
    public Task<List<Player>> AvailablePlayersAsync(int organizationId, int cupId) =>
        db.PlayerSet
            .AsNoTracking()
            .Where(p => p.CreatedInOrganizationId == organizationId && p.DeletedAt == null && !p.IsSystem
                        && !db.CupParticipantSet.Any(x => x.CupId == cupId && x.PlayerId == p.Id))
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

    public static List<CupStandingRow> Standings(Cup cup)
    {
        var finished = cup.Matches
            .Where(m => m.Status == CupMatchStatus.Finished && m.ParticipantAId is not null && m.ParticipantBId is not null && m.WinnerParticipantId is not null)
            .Select(m => new CupResultLine(m.ParticipantAId!.Value, m.ParticipantBId!.Value, m.ScoreA, m.ScoreB, m.Innings, m.HighRunA, m.HighRunB, m.WinnerParticipantId!.Value));
        var byes = cup.Matches
            .Where(m => m.Status == CupMatchStatus.Bye && m.WinnerParticipantId is not null)
            .Select(m => m.WinnerParticipantId!.Value);
        return CupLogic.Standings(cup.Participants.Select(p => p.Id), finished, byes);
    }

    // ---------------------------------------------------------------- tournament

    public async Task<Cup> UpsertAsync(
        int organizationId, int id, string name, string? description, DateOnly startDate, DateOnly? endDate,
        CupFormat format, int? maxInnings)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Turnuva adı gerekli.");
        if (name.Length > 200) throw new ArgumentException("Turnuva adı en fazla 200 karakter olabilir.");
        if (endDate is not null && endDate < startDate) throw new ArgumentException("Bitiş tarihi başlangıçtan önce olamaz.");
        if (maxInnings is < 1) throw new ArgumentException("El sınırı 1 veya daha büyük olmalı.");

        Cup? cup = null;
        if (id != 0)
        {
            cup = await db.CupSet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
                ?? throw new ArgumentException("Turnuva bulunamadı.");
            if (cup.Status != CupStatus.Draft && cup.Format != format)
            {
                throw new InvalidOperationException("Başlamış turnuvanın usulü değiştirilemez.");
            }
        }
        else
        {
            cup = new Cup { OrganizationId = organizationId, Status = CupStatus.Draft };
            db.CupSet.Add(cup);
        }

        cup.Name = name;
        cup.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        cup.StartDate = startDate;
        cup.EndDate = endDate;
        cup.Format = format;
        cup.MaxInnings = maxInnings;
        await db.SaveChangesAsync();
        return cup;
    }

    public async Task DeleteAsync(int organizationId, int id)
    {
        var cup = await db.CupSet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Turnuva bulunamadı.");
        cup.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- participants

    public async Task AddParticipantsAsync(int organizationId, int cupId, IEnumerable<int> playerIds)
    {
        var cup = await FindDraftAsync(organizationId, cupId);
        var ids = playerIds.Distinct().ToList();
        var players = await db.PlayerSet
            .Where(p => ids.Contains(p.Id) && p.CreatedInOrganizationId == organizationId && p.DeletedAt == null && !p.IsSystem)
            .ToListAsync();
        if (players.Count != ids.Count) throw new ArgumentException("Oyuncu bulunamadı.");

        var existing = await db.CupParticipantSet.Where(p => p.CupId == cupId).ToListAsync();
        var nextSeed = existing.Count == 0 ? 1 : existing.Max(p => p.Seed ?? 0) + 1;
        foreach (var player in players.Where(p => existing.All(e => e.PlayerId != p.Id)).OrderBy(p => p.DisplayName))
        {
            db.CupParticipantSet.Add(new CupParticipant
            {
                CupId = cup.Id,
                PlayerId = player.Id,
                HandicapTarget = player.DefaultTargetPoints,
                Seed = nextSeed++
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task RemoveParticipantAsync(int organizationId, int cupId, int participantId)
    {
        await FindDraftAsync(organizationId, cupId);
        var participant = await db.CupParticipantSet.FirstOrDefaultAsync(p => p.Id == participantId && p.CupId == cupId)
            ?? throw new InvalidOperationException("Katılımcı bulunamadı.");
        db.CupParticipantSet.Remove(participant);
        await db.SaveChangesAsync();
    }

    public async Task SetParticipantAsync(int organizationId, int cupId, int participantId, int? handicapTarget, int? seed)
    {
        await FindDraftAsync(organizationId, cupId);
        if (handicapTarget is < 1 or > 9999) throw new ArgumentException("Handikap sayısı 1-9999 arasında olmalı.");
        if (seed is < 1) throw new ArgumentException("Seri başı sırası 1 veya daha büyük olmalı.");
        var participant = await db.CupParticipantSet.FirstOrDefaultAsync(p => p.Id == participantId && p.CupId == cupId)
            ?? throw new InvalidOperationException("Katılımcı bulunamadı.");
        participant.HandicapTarget = handicapTarget;
        participant.Seed = seed;
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- round rules

    public async Task SetRuleBlocksAsync(int organizationId, int cupId, IEnumerable<RuleBlockInput> blocks)
    {
        var cup = await FindDraftAsync(organizationId, cupId);
        var list = blocks.OrderBy(b => b.FromRound).ThenBy(b => b.ToRound).ToList();
        ValidateBlocks(list);

        db.CupRuleBlockSet.RemoveRange(await db.CupRuleBlockSet.Where(b => b.CupId == cupId).ToListAsync());
        foreach (var block in list)
        {
            db.CupRuleBlockSet.Add(new CupRuleBlock
            {
                CupId = cup.Id,
                FromRound = block.FromRound,
                ToRound = block.ToRound,
                Mode = block.Mode,
                FixedTarget = block.Mode == CupRuleMode.Fixed ? block.FixedTarget : null
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>"First N rounds handicap, the remaining rounds fixed at M" in one call (handicapRounds 0 = all fixed, ≥ rounds = all handicap).</summary>
    public async Task QuickRulesAsync(int organizationId, int cupId, int handicapRounds, int? fixedTarget)
    {
        var cup = await FindDraftAsync(organizationId, cupId);
        var participants = await db.CupParticipantSet.CountAsync(p => p.CupId == cupId);
        var rounds = CupLogic.TotalRounds(cup.Format, participants);
        if (rounds == 0) throw new InvalidOperationException("Önce en az iki katılımcı ekleyin.");
        if (handicapRounds < rounds && fixedTarget is null or < 1) throw new ArgumentException("Sabit hedef sayısı gerekli.");

        await SetRuleBlocksAsync(organizationId, cupId, CupLogic.QuickBlocks(rounds, handicapRounds, fixedTarget));
    }

    private static void ValidateBlocks(List<RuleBlockInput> list)
    {
        foreach (var block in list)
        {
            if (block.FromRound < 1 || block.ToRound < block.FromRound)
            {
                throw new ArgumentException("Tur aralığı geçersiz: başlangıç 1 veya daha büyük, bitiş başlangıçtan küçük olamaz.");
            }

            if (block.Mode == CupRuleMode.Fixed && block.FixedTarget is null or < 1 or > 9999)
            {
                throw new ArgumentException("Sabit hedef sayısı 1-9999 arasında olmalı.");
            }
        }

        for (var i = 1; i < list.Count; i++)
        {
            if (list[i].FromRound <= list[i - 1].ToRound)
            {
                throw new LocalizedArgumentException("{0}. tur birden fazla kural aralığında.", list[i].FromRound);
            }
        }
    }

    // ---------------------------------------------------------------- start / reset

    public async Task StartAsync(int organizationId, int cupId)
    {
        var cup = await FindDraftAsync(organizationId, cupId);
        var participants = await db.CupParticipantSet
            .Include(p => p.Player)
            .Where(p => p.CupId == cupId)
            .ToListAsync();
        if (participants.Count < 2) throw new InvalidOperationException("Turnuvayı başlatmak için en az iki katılımcı gerekli.");

        var blocks = (await db.CupRuleBlockSet.Where(b => b.CupId == cupId).ToListAsync())
            .Select(b => new RuleBlockInput(b.FromRound, b.ToRound, b.Mode, b.FixedTarget)).ToList();
        var rounds = CupLogic.TotalRounds(cup.Format, participants.Count);
        var (uncovered, overlapping) = CupLogic.CheckCoverage(blocks, rounds);
        if (uncovered.Count > 0) throw new LocalizedArgumentException("Şu turlar için kural tanımlı değil: {0}.", string.Join(", ", uncovered));
        if (overlapping.Count > 0) throw new LocalizedArgumentException("{0}. tur birden fazla kural aralığında.", string.Join(", ", overlapping));

        var handicapRounds = Enumerable.Range(1, rounds)
            .Any(r => blocks.First(b => r >= b.FromRound && r <= b.ToRound).Mode == CupRuleMode.Handicap);
        if (handicapRounds)
        {
            var missing = participants.Where(p => p.HandicapTarget is null).Select(p => p.Player.DisplayName).ToList();
            if (missing.Count > 0)
            {
                throw new LocalizedArgumentException("Handikaplı turlar var; şu katılımcılar için handikap sayısı girilmeli: {0}.", string.Join(", ", missing));
            }
        }

        var ordered = participants.OrderBy(p => p.Seed ?? int.MaxValue).ThenBy(p => p.Id).ToList();
        var matches = cup.Format == CupFormat.RoundRobin
            ? BuildRoundRobin(cup, ordered, blocks)
            : BuildElimination(cup, ordered, blocks, rounds);

        db.CupMatchSet.AddRange(matches);
        cup.Status = CupStatus.Running;
        cup.TotalRounds = rounds;
        await db.SaveChangesAsync();
    }

    public async Task ResetAsync(int organizationId, int cupId)
    {
        var cup = await db.CupSet.FirstOrDefaultAsync(c => c.Id == cupId && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Turnuva bulunamadı.");
        db.CupMatchSet.RemoveRange(await db.CupMatchSet.Where(m => m.CupId == cupId).ToListAsync());
        cup.Status = CupStatus.Draft;
        cup.TotalRounds = 0;
        await db.SaveChangesAsync();
    }

    private static List<CupMatch> BuildRoundRobin(Cup cup, List<CupParticipant> ordered, List<RuleBlockInput> blocks)
    {
        var matches = new List<CupMatch>();
        var schedule = CupLogic.RoundRobin(ordered.Count);
        for (var r = 0; r < schedule.Count; r++)
        {
            for (var i = 0; i < schedule[r].Count; i++)
            {
                var (a, b) = schedule[r][i];
                var match = new CupMatch { CupId = cup.Id, Round = r + 1, Number = i + 1, ParticipantAId = ordered[a].Id };
                if (b is null)
                {
                    match.Status = CupMatchStatus.Bye;
                    match.WinnerParticipantId = ordered[a].Id;
                }
                else
                {
                    match.ParticipantBId = ordered[b.Value].Id;
                    match.Status = CupMatchStatus.Scheduled;
                    (match.TargetA, match.TargetB) = CupLogic.ResolveTargets(blocks, match.Round, ordered[a].HandicapTarget, ordered[b.Value].HandicapTarget);
                }

                matches.Add(match);
            }
        }

        return matches;
    }

    private static List<CupMatch> BuildElimination(Cup cup, List<CupParticipant> ordered, List<RuleBlockInput> blocks, int rounds)
    {
        var size = CupLogic.BracketSize(ordered.Count);
        var all = new List<CupMatch>();
        for (var r = 1; r <= rounds; r++)
        {
            for (var n = 1; n <= size >> r; n++)
            {
                all.Add(new CupMatch { CupId = cup.Id, Round = r, Number = n, Status = CupMatchStatus.Scheduled });
            }
        }

        var firstRound = CupLogic.EliminationFirstRound(ordered.Count);
        for (var i = 0; i < firstRound.Count; i++)
        {
            var match = all.First(m => m.Round == 1 && m.Number == i + 1);
            var (seedA, seedB) = firstRound[i];
            match.ParticipantAId = seedA is null ? null : ordered[seedA.Value - 1].Id;
            match.ParticipantBId = seedB is null ? null : ordered[seedB.Value - 1].Id;
        }

        var byId = ordered.ToDictionary(p => p.Id);
        foreach (var match in all.Where(m => m.Round == 1))
        {
            if (match.ParticipantAId is not null && match.ParticipantBId is not null)
            {
                ResolveTargets(match, byId, blocks);
            }
            else
            {
                // A bye: the only participant advances at once.
                match.Status = CupMatchStatus.Bye;
                match.WinnerParticipantId = match.ParticipantAId ?? match.ParticipantBId;
                Advance(all, match, byId, blocks, rounds);
            }
        }

        return all;
    }

    private static void ResolveTargets(CupMatch match, Dictionary<int, CupParticipant> byId, List<RuleBlockInput> blocks)
    {
        var a = match.ParticipantAId is { } ai ? byId[ai].HandicapTarget : null;
        var b = match.ParticipantBId is { } bi ? byId[bi].HandicapTarget : null;
        (match.TargetA, match.TargetB) = CupLogic.ResolveTargets(blocks, match.Round, a, b);
    }

    /// <summary>Puts the winner of `match` into its slot of the next round and resolves that match's targets once both are known.</summary>
    private static void Advance(IEnumerable<CupMatch> all, CupMatch match, Dictionary<int, CupParticipant> byId, List<RuleBlockInput> blocks, int totalRounds)
    {
        if (match.Round >= totalRounds || match.WinnerParticipantId is null) return;
        var (number, isSlotA) = CupLogic.NextSlot(match.Number);
        var next = all.First(m => m.Round == match.Round + 1 && m.Number == number);
        if (isSlotA) next.ParticipantAId = match.WinnerParticipantId; else next.ParticipantBId = match.WinnerParticipantId;
        if (next.ParticipantAId is not null && next.ParticipantBId is not null)
        {
            ResolveTargets(next, byId, blocks);
        }
    }

    // ---------------------------------------------------------------- results

    public async Task RecordResultAsync(
        int organizationId, int matchId, int scoreA, int scoreB, int innings, int highRunA, int highRunB, int winnerParticipantId)
    {
        var (cup, match, all) = await LoadMatchAsync(organizationId, matchId);
        if (match.Status == CupMatchStatus.Bye || match.ParticipantAId is null || match.ParticipantBId is null)
        {
            throw new InvalidOperationException("Bu maçın oyuncuları henüz belli değil.");
        }

        if (scoreA < 0 || scoreB < 0 || innings < 0 || highRunA < 0 || highRunB < 0) throw new ArgumentException("Sayılar negatif olamaz.");
        if (winnerParticipantId != match.ParticipantAId && winnerParticipantId != match.ParticipantBId) throw new ArgumentException("Kazanan seçin.");

        var isElimination = cup.Format == CupFormat.SingleElimination;
        var next = isElimination && match.Round < cup.TotalRounds ? NextMatch(all, match) : null;
        if (next is not null && next.Status == CupMatchStatus.Finished)
        {
            throw new InvalidOperationException("Sonraki tur maçı oynandığı için bu sonuç değiştirilemez.");
        }

        match.ScoreA = scoreA;
        match.ScoreB = scoreB;
        match.Innings = innings;
        match.HighRunA = highRunA;
        match.HighRunB = highRunB;
        match.WinnerParticipantId = winnerParticipantId;
        match.Status = CupMatchStatus.Finished;
        match.PlayedAt = DateTimeOffset.UtcNow;

        if (next is not null)
        {
            var participants = await db.CupParticipantSet.Where(p => p.CupId == cup.Id).ToDictionaryAsync(p => p.Id);
            Advance(all, match, participants, await BlocksAsync(cup.Id), cup.TotalRounds);
        }

        await CompleteIfDoneAsync(cup, all);
        await db.SaveChangesAsync();
    }

    public async Task ClearResultAsync(int organizationId, int matchId)
    {
        var (cup, match, all) = await LoadMatchAsync(organizationId, matchId);
        if (match.Status != CupMatchStatus.Finished) return;

        if (cup.Format == CupFormat.SingleElimination && match.Round < cup.TotalRounds)
        {
            var next = NextMatch(all, match)!;
            if (next.Status == CupMatchStatus.Finished)
            {
                throw new InvalidOperationException("Sonraki tur maçı oynandığı için bu sonuç değiştirilemez.");
            }

            if (CupLogic.NextSlot(match.Number).IsSlotA) next.ParticipantAId = null; else next.ParticipantBId = null;
            next.TargetA = next.TargetB = null;
        }

        match.Status = CupMatchStatus.Scheduled;
        match.ScoreA = match.ScoreB = match.Innings = match.HighRunA = match.HighRunB = 0;
        match.WinnerParticipantId = null;
        match.PlayedAt = null;
        cup.Status = CupStatus.Running;
        await db.SaveChangesAsync();
    }

    public async Task SetTableAsync(int organizationId, int matchId, int? tableId)
    {
        var (_, match, _) = await LoadMatchAsync(organizationId, matchId);
        if (tableId is not null &&
            !await db.BilliardTableSet.AnyAsync(t => t.Id == tableId && t.OrganizationId == organizationId && t.DeletedAt == null))
        {
            throw new InvalidOperationException("Masa bulunamadı.");
        }

        match.TableId = tableId;
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- helpers

    private async Task<Cup> FindDraftAsync(int organizationId, int cupId)
    {
        var cup = await db.CupSet.FirstOrDefaultAsync(c => c.Id == cupId && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Turnuva bulunamadı.");
        if (cup.Status != CupStatus.Draft) throw new InvalidOperationException("Turnuva başladıktan sonra değiştirilemez. Önce sıfırlayın.");
        return cup;
    }

    private async Task<(Cup Cup, CupMatch Match, List<CupMatch> All)> LoadMatchAsync(int organizationId, int matchId)
    {
        var match = await db.CupMatchSet.Include(m => m.Cup)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.Cup.OrganizationId == organizationId && m.Cup.DeletedAt == null)
            ?? throw new InvalidOperationException("Maç bulunamadı.");
        var all = await db.CupMatchSet.Where(m => m.CupId == match.CupId).ToListAsync();
        // Reuse the tracked instance for the requested match.
        return (match.Cup, all.First(m => m.Id == matchId), all);
    }

    private static CupMatch? NextMatch(IEnumerable<CupMatch> all, CupMatch match)
    {
        var (number, _) = CupLogic.NextSlot(match.Number);
        return all.FirstOrDefault(m => m.Round == match.Round + 1 && m.Number == number);
    }

    private async Task<List<RuleBlockInput>> BlocksAsync(int cupId) =>
        (await db.CupRuleBlockSet.Where(b => b.CupId == cupId).ToListAsync())
            .Select(b => new RuleBlockInput(b.FromRound, b.ToRound, b.Mode, b.FixedTarget)).ToList();

    private static Task CompleteIfDoneAsync(Cup cup, List<CupMatch> all)
    {
        var done = all.All(m => m.Status is CupMatchStatus.Finished or CupMatchStatus.Bye);
        if (done && cup.Status == CupStatus.Running)
        {
            cup.Status = CupStatus.Finished;
            cup.EndDate ??= DateOnly.FromDateTime(DateTime.Today);
        }
        else if (!done && cup.Status == CupStatus.Finished)
        {
            cup.Status = CupStatus.Running;
        }

        return Task.CompletedTask;
    }
}
