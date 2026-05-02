using Plugin.Maui.OCR;
using System.Text.RegularExpressions;

namespace BilliardIQ.Mobile.Services;

/// <summary>
/// Ball the user plays with — null means the scoreboard didn't indicate.
/// </summary>
public enum PlayerBall { White, Yellow }

/// <param name="Player1Score">Left / Thuis / home player current score.</param>
/// <param name="Player2Score">Right / Gasten / away player current score.</param>
/// <param name="Innings">Number of visits played so far.</param>
/// <param name="HighestRun">Longest unbroken run (en yüksek seri / HS).</param>
/// <param name="Average">Current billiard average, points per inning (ortalama / Gem).</param>
/// <param name="PlayerBall">Ball the *first* player uses, if determinable from the layout.</param>
public record OcrDetectedValues(
    int Player1Score,
    int Player2Score,
    int? Innings = null,
    int? HighestRun = null,
    double? Average = null,
    PlayerBall? PlayerBall = null);

public partial class ScoreboardOcrService
{
    public static Task<OcrDetectedValues?> ExtractValuesAsync(OcrResult ocrResult)
    {
        try
        {
            if (!ocrResult.Success || string.IsNullOrWhiteSpace(ocrResult.AllText))
                return Task.FromResult<OcrDetectedValues?>(null);

            return Task.FromResult(Parse(ocrResult.AllText));
        }
        catch
        {
            return Task.FromResult<OcrDetectedValues?>(null);
        }
    }

    private static OcrDetectedValues? Parse(string raw)
    {
        var lower = raw.ToLowerInvariant();

        // Dutch indicators: Gasten (away/yellow) vs Thuis (home/white)
        if (lower.Contains("gasten") || lower.Contains("thuis") || lower.Contains("beurt"))
            return ParseDutch(raw);

        // Turkish / English digital boards: PLAYER 1 | TARGET | PLAYER 2
        if (lower.Contains("player") || lower.Contains("oyuncu") ||
            lower.Contains("seri") || lower.Contains("çekim") || lower.Contains("ortalama"))
            return ParseTurkish(raw);

        return ParseGeneric(raw);
    }

    // ── Dutch vertical layout ────────────────────────────────────────────────
    // Header colour = ball colour: Gasten (yellow header) = yellow ball,
    //                              Thuis  (white  header) = white  ball.
    private static OcrDetectedValues? ParseDutch(string text)
    {
        var lines = SplitLines(text);

        int gastenIdx = LineContaining(lines, "gasten");
        int thuisIdx  = LineContaining(lines, "thuis");

        if (gastenIdx < 0 && thuisIdx < 0)
            return ParseGeneric(text);

        int? gastenScore = null, gastenTarget = null, gastenHS = null;
        int? thuisScore  = null, thuisTarget  = null, thuisHS  = null;
        int? innings     = null;
        double? thuisAvg = null, gastenAvg = null;

        if (gastenIdx >= 0)
            ExtractDutchSection(lines, gastenIdx,
                ref gastenScore, ref gastenTarget, ref gastenHS, ref gastenAvg);

        if (thuisIdx >= 0)
            ExtractDutchSection(lines, thuisIdx,
                ref thuisScore, ref thuisTarget, ref thuisHS, ref thuisAvg);

        // Use the user's (Thuis/Player1) highest run; fall back to Gasten's
        int? highestRun = thuisHS ?? gastenHS;

        var beurtM = BeurtRegex().Match(text);
        if (beurtM.Success && int.TryParse(beurtM.Groups[1].Value, out var bv))
            innings = bv;

        // P1 = Thuis (home, white), P2 = Gasten (away, yellow)
        // Do NOT fall back p1 to gastenScore — they'd appear equal when only one
        // section was parsed, which is misleading.
        int p1 = thuisScore  ?? 0;
        int p2 = gastenScore ?? 0;
        if (p1 == 0 && p2 == 0) return null;

        var ball = thuisScore.HasValue  ? Services.PlayerBall.White
                 : gastenScore.HasValue ? Services.PlayerBall.Yellow
                 : (PlayerBall?)null;

        // Back-calculate innings from score ÷ average — more reliable than
        // looking for "beurt" keyword which may not appear in the OCR text.
        if (innings == null && p1 > 0 && thuisAvg is > 0.001)
            innings = (int)Math.Round(p1 / thuisAvg.Value);
        else if (innings == null && p2 > 0 && gastenAvg is > 0.001)
            innings = (int)Math.Round(p2 / gastenAvg.Value);

        return new OcrDetectedValues(p1, p2,
            Innings:    innings,
            HighestRun: highestRun,
            Average:    thuisAvg ?? gastenAvg,
            PlayerBall: ball);
    }

    private static void ExtractDutchSection(string[] lines, int labelIdx,
        ref int? score, ref int? target, ref int? highestRun, ref double? average)
    {
        if (labelIdx < 0) return;

        // Collect score/target candidates from the label line + following lines.
        // Key: stop adding candidates once we have 2 (target + score), but keep
        // scanning so we still reach the stats line (Gem / HS).
        var candidates = new List<int>();

        int kwLen = Math.Min(6, lines[labelIdx].Length);
        candidates.AddRange(AllInts(lines[labelIdx][kwLen..]));

        for (int i = labelIdx + 1; i < Math.Min(labelIdx + 8, lines.Length); i++)
        {
            var ll = lines[i].ToLowerInvariant();

            if (ll.Contains("gasten") || ll.Contains("thuis")) break;

            if (ll.Contains("gem") || ll.Contains("hs"))
            {
                highestRun ??= IntAfterLabel(lines[i], @"\bhs\b[^\d]{0,5}(\d{1,3})");
                average    ??= FirstDecimal(lines[i]);
                break;
            }

            // Only add to candidates until we have target + score (2 numbers).
            // Lines beyond that are inning-table data we don't want to misread.
            if (candidates.Count < 2)
                candidates.AddRange(AllInts(lines[i]));
        }

        if (candidates.Count == 0) return;
        if (candidates.Count == 1) { score ??= candidates[0]; return; }

        // Classify the first two candidates as (target, score).
        // In a live game: current score ≤ target (allow small final-inning overshoot).
        int first = candidates[0], second = candidates[1];
        if (second <= first + 5) { target ??= first;                    score ??= second; }
        else                      { score  ??= Math.Min(first, second); target ??= Math.Max(first, second); }
    }

    // ── Turkish / English horizontal layout ──────────────────────────────────
    // Scoreboard reads left-to-right: [P1 score] [TARGET in red box] [P2 score]
    // PLAYER 1 conventionally uses the white ball, PLAYER 2 the yellow.
    private static OcrDetectedValues? ParseTurkish(string text)
    {
        var lines = SplitLines(text);

        int? p1 = null, p2 = null, target = null;
        int? innings = null, highestRun = null;
        double? average = null;

        // Collect labeled stats from any line
        foreach (var line in lines)
        {
            var ll = line.ToLowerInvariant();

            if (ll.Contains("seri") || ll.Contains("serie") || ll.Contains("high run"))
                highestRun ??= IntAfterLabel(line, @"(?:seri|serie|high\s*run)[^\d]{0,10}(\d{1,3})");

            if (ll.Contains("çekim") || ll.Contains("cekim") || ll.Contains("innings") || ll.Contains("beurt"))
                innings ??= IntAfterLabel(line, @"(?:çekim|cekim|innings?|beurt)[^\d]{0,10}(\d{1,4})");

            if (ll.Contains("ortalama") || ll.Contains("average") || ll.Contains("avg"))
                average ??= FirstDecimal(line);
        }

        // Find main score line: [P1] [TARGET] [P2] left to right.
        // Target heuristic: middle of three numbers, 10–99 range,
        // both flanking scores within +20 of it (catches "game over" overshoots).
        foreach (var line in lines)
        {
            var nums = AllInts(line);
            if (nums.Count < 3) continue;

            // Slide a window and test each position as the target
            for (int i = 1; i < nums.Count - 1; i++)
            {
                int left   = nums[i - 1];
                int center = nums[i];
                int right  = nums[i + 1];

                if (IsPlausibleTarget(center)
                    && left  <= center + 20
                    && right <= center + 20)
                {
                    p1 = left; target = center; p2 = right;
                    break;
                }
            }
            if (p1 != null) break;
        }

        if (p1 == null)
            return ParseGeneric(text, innings, highestRun, average);

        return new OcrDetectedValues(p1.Value, p2 ?? 0,
            innings, highestRun, average,
            PlayerBall: Services.PlayerBall.White);
    }

    // ── Generic fallback ─────────────────────────────────────────────────────
    private static OcrDetectedValues? ParseGeneric(string text,
        int? innings = null, int? highestRun = null, double? average = null)
    {
        // Explicit separator: "19 – 18" / "19:18" / "19-18"
        var sepM = SeparatorRegex().Match(text);
        if (sepM.Success
            && int.TryParse(sepM.Groups[1].Value, out var ps)
            && int.TryParse(sepM.Groups[2].Value, out var os))
        {
            innings ??= FirstInt(text[(sepM.Index + sepM.Length)..]);
            return new OcrDetectedValues(ps, os, innings, highestRun, average);
        }

        // Last resort: first two distinct numbers
        var all = AllInts(text);
        if (all.Count < 2) return null;

        innings ??= all.Count >= 3 ? all[2] : null;
        return new OcrDetectedValues(all[0], all[1], innings, highestRun, average);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string[] SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int LineContaining(string[] lines, string keyword)
    {
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    // Skips the first `skipChars` non-digit characters, then returns the first int.
    // Used to skip the keyword itself on the label line (e.g. "Gasten 18" → 18).
    private static int? FirstIntSkipping(string line, int skipChars)
    {
        var cleaned = line.Length > skipChars ? line[skipChars..] : line;
        return FirstInt(cleaned);
    }

    private static int? FirstInt(string text)
    {
        var m = IntTokenRegex().Match(text);
        return m.Success && int.TryParse(m.Value, out var v) ? v : null;
    }

    private static int? IntAfterLabel(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    private static double? FirstDecimal(string text)
    {
        var m = DecimalRegex().Match(text);
        if (!m.Success) return null;
        var normalized = m.Value.Replace(',', '.');
        return double.TryParse(normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v : null;
    }

    private static List<int> AllInts(string text) =>
        IntTokenRegex().Matches(text)
            .Select(m => int.Parse(m.Value))
            .Where(n => n is >= 0 and <= 999)
            .ToList();

    // 3-cushion targets: typically 15–60, always ≤ 99
    private static bool IsPlausibleTarget(int n) => n is >= 10 and <= 99;

    [GeneratedRegex(@"\b\d{1,3}\b")]
    private static partial Regex IntTokenRegex();

    [GeneratedRegex(@"(\d{1,3})\s*[-–:]\s*(\d{1,3})")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"\d+[,\.]\d{1,4}")]
    private static partial Regex DecimalRegex();

    [GeneratedRegex(@"\bbeurt\b[^\d]{0,5}(\d{1,3})", RegexOptions.IgnoreCase)]
    private static partial Regex BeurtRegex();
}
