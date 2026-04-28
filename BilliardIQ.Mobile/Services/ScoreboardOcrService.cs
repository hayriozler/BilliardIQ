using Plugin.Maui.OCR;
using System.Text.RegularExpressions;

namespace BilliardIQ.Mobile.Services;

public record OcrDetectedValues(int Player, int Opponent, int? Innings = null, int? HighestRun = null);

public partial class ScoreboardOcrService(IOcrService OcrService)
{
    public async Task<OcrDetectedValues?> ExtractValuesAsync(string photoPath)
    {
        if (!File.Exists(photoPath)) return null;

        try
        {
            await OcrService.InitAsync();
            var imageBytes = await File.ReadAllBytesAsync(photoPath);
            var result = await OcrService.RecognizeTextAsync(imageBytes, tryHard: true);

            if (!result.Success || string.IsNullOrWhiteSpace(result.AllText))
                return null;

            return Parse(result.AllText);
        }
        catch
        {
            return null;
        }
    }

    internal static OcrDetectedValues? Parse(string rawText)
    {
        // Normalize whitespace but keep line structure for positional parsing
        var text = InningsRegex().Replace(rawText, " ").Trim();

        // ── 1. Innings: look for labelled pattern first (highest confidence) ─────
        int? innings = null;
        var inningsMatch = InningsRegex().Match(text);
        if (inningsMatch.Success && int.TryParse(inningsMatch.Groups[1].Value, out var il))
            innings = il;

        // ── 2. Highest run: look for labelled pattern ─────────────────────────────
        int? highestRun = null;
        var runMatch = HighRunRegext().Match(text);
        if (runMatch.Success && int.TryParse(runMatch.Groups[1].Value, out var rl))
            highestRun = rl;

        // ── 3. Scores: try explicit separator "46 - 50" / "46:50" / "46–50" ───────
        var sepMatch = ResultRegex().Match(text);
        if (sepMatch.Success
            && int.TryParse(sepMatch.Groups[1].Value, out var ps)
            && int.TryParse(sepMatch.Groups[2].Value, out var os))
        {
            // If innings not labelled, try the next standalone number after scores
            innings ??= FindInningsAfterScores(text, sepMatch.Index + sepMatch.Length);

            return new OcrDetectedValues(ps, os, innings, highestRun);
        }

        // ── 4. Fallback: collect all 1-3 digit numbers ───────────────────────────
        var allNums = FallbackRegex().Matches(text)
            .Select(m => (Value: int.Parse(m.Groups[1].Value), Index: m.Index))
            .Where(n => n.Value is >= 0 and <= 999)
            .ToList();

        if (allNums.Count < 2) return null;

        var player   = allNums[0].Value;
        var opponent = allNums[1].Value;

        // Third number as innings candidate if not already found via label
        if (innings is null && allNums.Count >= 3)
            innings = allNums[2].Value;

        return new OcrDetectedValues(player, opponent, innings, highestRun);
    }

    // After the score match, look for the next isolated number (candidate innings)
    private static int? FindInningsAfterScores(string text, int startIndex)
    {
        var remaining = text[startIndex..];
        var m = InningsRegex().Match(remaining);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v))
            return v;
        return null;
    }

    [GeneratedRegex(@"\b(\d{1,3})\b")]
    private static partial Regex InningsRegex();

    [GeneratedRegex(@"(?:serie|seri|high\s*run|en\s*y[üu]ksek)[^\d]{0,5}(\d{1,3})", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex HighRunRegext();

    [GeneratedRegex(@"(\d{1,3})\s*[-–:]\s*(\d{1,3})")]
    private static partial Regex ResultRegex();
    [GeneratedRegex(@"\b(\d{1,3})\b")]
    private static partial Regex FallbackRegex();
}
