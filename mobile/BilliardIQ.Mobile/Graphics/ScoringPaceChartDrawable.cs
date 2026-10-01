namespace BillardIQ.Mobile.Graphics;

// Bar chart of points scored per fixed-width time bucket (e.g. every 5 minutes) within a single
// match. Values must already be zero-filled/dense — match_score_stat rows are sparse (no row for
// an empty bucket), so the caller fills gaps before assigning BucketPoints.
public sealed class ScoringPaceChartDrawable : IDrawable
{
    public IReadOnlyList<int> BucketPoints { get; set; } = [];
    public int BucketMinutes { get; set; } = 5;
    public Color BarColor { get; set; } = Color.FromArgb("#1565C0");
    public Color AxisColor { get; set; } = Color.FromArgb("#B0BEC5");
    public Color LabelColor { get; set; } = Color.FromArgb("#78909C");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BucketPoints.Count == 0) return;

        const float padding = 24f;
        const float labelHeight = 16f;
        var width = dirtyRect.Width - padding * 2;
        var height = dirtyRect.Height - padding - labelHeight;
        if (width <= 0 || height <= 0) return;

        var max = Math.Max(1, BucketPoints.Max());
        var baseline = padding + height;

        canvas.StrokeColor = AxisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(padding, baseline, dirtyRect.Width - padding, baseline);

        var slot = width / BucketPoints.Count;
        var barWidth = Math.Min(28f, slot * 0.6f);

        // The Windows ICanvas backend NREs inside DrawString if Font is left unset — FontSize/FontColor
        // alone aren't enough, an explicit IFont is required.
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 9;
        canvas.FontColor = LabelColor;
        for (var i = 0; i < BucketPoints.Count; i++)
        {
            var barHeight = BucketPoints[i] / (float)max * height;
            var x = padding + i * slot + (slot - barWidth) / 2;
            var y = baseline - barHeight;

            canvas.FillColor = BarColor;
            canvas.FillRoundedRectangle(x, y, barWidth, Math.Max(barHeight, 1), 3);

            canvas.DrawString((i * BucketMinutes).ToString(), padding + i * slot, baseline + 2, slot, labelHeight,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
