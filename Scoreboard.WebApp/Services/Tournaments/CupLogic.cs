namespace Scoreboard.WebApp.Services.Tournaments;

public record CupStandingRow(
    int ParticipantId, int Played, int Won, int Lost, int MatchPoints,
    int PointsFor, int PointsAgainst, int Innings, double Average, int BestHighRun);

public record RuleBlockInput(int FromRound, int ToRound, CupRuleMode Mode, int? FixedTarget);

public record CupResultLine(
    int ParticipantAId, int ParticipantBId, int ScoreA, int ScoreB, int Innings, int HighRunA, int HighRunB, int WinnerParticipantId);

public static class CupLogic
{
    public const int WinPoints = 2;

    public static int TotalRounds(CupFormat format, int participants)
    {
        if (participants < 2) return 0;
        return format == CupFormat.RoundRobin
            ? (participants % 2 == 0 ? participants - 1 : participants)
            : (int)Math.Ceiling(Math.Log2(participants));
    }

    public static List<List<(int A, int? B)>> RoundRobin(int n)
    {
        var rounds = new List<List<(int A, int? B)>>();
        if (n < 2) return rounds;

        var slots = Enumerable.Range(0, n).Select(i => (int?)i).ToList();
        if (n % 2 == 1) slots.Add(null);
        var size = slots.Count;

        for (var round = 0; round < size - 1; round++)
        {
            var pairs = new List<(int A, int? B)>();
            for (var i = 0; i < size / 2; i++)
            {
                var first = slots[i];
                var second = slots[size - 1 - i];
                if (first is null) pairs.Add((second!.Value, null));
                else pairs.Add((first.Value, second));
            }

            rounds.Add(pairs);

            var last = slots[^1];
            slots.RemoveAt(size - 1);
            slots.Insert(1, last);
        }

        return rounds;
    }

    public static int BracketSize(int n)
    {
        var size = 1;
        while (size < n) size <<= 1;
        return size;
    }

    public static List<int> BracketOrder(int size)
    {
        var order = new List<int> { 1 };
        while (order.Count < size)
        {
            var next = order.Count * 2;
            order = order.SelectMany(s => new[] { s, next + 1 - s }).ToList();
        }

        return order;
    }

    public static List<(int? SeedA, int? SeedB)> EliminationFirstRound(int participants)
    {
        var size = BracketSize(participants);
        var order = BracketOrder(size);
        var pairs = new List<(int? SeedA, int? SeedB)>();
        for (var i = 0; i < order.Count; i += 2)
        {
            int? a = order[i] <= participants ? order[i] : null;
            int? b = order[i + 1] <= participants ? order[i + 1] : null;
            pairs.Add((a, b));
        }

        return pairs;
    }

    public static (int Number, bool IsSlotA) NextSlot(int number) => ((number + 1) / 2, number % 2 == 1);

    public static (List<int> Uncovered, List<int> Overlapping) CheckCoverage(IEnumerable<RuleBlockInput> blocks, int totalRounds)
    {
        var count = new int[totalRounds + 1];
        foreach (var block in blocks)
        {
            for (var round = Math.Max(1, block.FromRound); round <= Math.Min(totalRounds, block.ToRound); round++)
            {
                count[round]++;
            }
        }

        var uncovered = Enumerable.Range(1, totalRounds).Where(r => count[r] == 0).ToList();
        var overlapping = Enumerable.Range(1, totalRounds).Where(r => count[r] > 1).ToList();
        return (uncovered, overlapping);
    }

    public static (int? TargetA, int? TargetB) ResolveTargets(
        IEnumerable<RuleBlockInput> blocks, int round, int? handicapA, int? handicapB)
    {
        var block = blocks.FirstOrDefault(b => round >= b.FromRound && round <= b.ToRound);
        if (block is null) return (null, null);
        return block.Mode == CupRuleMode.Fixed
            ? (block.FixedTarget, block.FixedTarget)
            : (handicapA, handicapB);
    }

    public static List<RuleBlockInput> QuickBlocks(int totalRounds, int handicapRounds, int? fixedTarget)
    {
        handicapRounds = Math.Clamp(handicapRounds, 0, totalRounds);
        var blocks = new List<RuleBlockInput>();
        if (handicapRounds > 0)
        {
            blocks.Add(new RuleBlockInput(1, handicapRounds, CupRuleMode.Handicap, null));
        }

        if (handicapRounds < totalRounds)
        {
            blocks.Add(new RuleBlockInput(handicapRounds + 1, totalRounds, CupRuleMode.Fixed, fixedTarget));
        }

        return blocks;
    }

    public static int SuggestWinner(int scoreA, int scoreB, int? targetA, int? targetB)
    {
        var reachedA = targetA is > 0 && scoreA >= targetA;
        var reachedB = targetB is > 0 && scoreB >= targetB;
        if (reachedA && !reachedB) return 1;
        if (reachedB && !reachedA) return 2;

        double ratioA = targetA is > 0 ? (double)scoreA / targetA.Value : scoreA;
        double ratioB = targetB is > 0 ? (double)scoreB / targetB.Value : scoreB;
        if (Math.Abs(ratioA - ratioB) < 1e-9) return 0;
        return ratioA > ratioB ? 1 : 2;
    }

    public static List<CupStandingRow> Standings(IEnumerable<int> participantIds, IEnumerable<CupResultLine> results, IEnumerable<int> byeWinners)
    {
        var rows = participantIds.ToDictionary(id => id, id => new Acc());

        foreach (var id in byeWinners)
        {
            if (!rows.TryGetValue(id, out var acc)) continue;
            acc.Won++;
            acc.MatchPoints += WinPoints;
        }

        foreach (var r in results)
        {
            if (rows.TryGetValue(r.ParticipantAId, out var a))
            {
                a.Add(r.ScoreA, r.ScoreB, r.Innings, r.HighRunA, r.WinnerParticipantId == r.ParticipantAId);
            }

            if (rows.TryGetValue(r.ParticipantBId, out var b))
            {
                b.Add(r.ScoreB, r.ScoreA, r.Innings, r.HighRunB, r.WinnerParticipantId == r.ParticipantBId);
            }
        }

        return rows
            .Select(kv => new CupStandingRow(
                kv.Key, kv.Value.Played, kv.Value.Won, kv.Value.Lost, kv.Value.MatchPoints,
                kv.Value.PointsFor, kv.Value.PointsAgainst, kv.Value.Innings,
                kv.Value.Innings == 0 ? 0 : Math.Round((double)kv.Value.PointsFor / kv.Value.Innings, 3), kv.Value.BestHighRun))
            .OrderByDescending(r => r.MatchPoints)
            .ThenByDescending(r => r.Average)
            .ThenByDescending(r => r.PointsFor - r.PointsAgainst)
            .ThenBy(r => r.ParticipantId)
            .ToList();
    }

    private sealed class Acc
    {
        public int Played, Won, Lost, MatchPoints, PointsFor, PointsAgainst, Innings, BestHighRun;

        public void Add(int scoreFor, int scoreAgainst, int innings, int highRun, bool won)
        {
            Played++;
            if (won) { Won++; MatchPoints += WinPoints; } else Lost++;
            PointsFor += scoreFor;
            PointsAgainst += scoreAgainst;
            Innings += innings;
            BestHighRun = Math.Max(BestHighRun, highRun);
        }
    }
}
