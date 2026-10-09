namespace BilliardIQ.Mobile.Graphics;

public sealed class TrendChartDrawable : IDrawable
{
    public IReadOnlyList<double> Values { get; set; } = [];
    public string ValueFormat { get; set; } = "F2";
    public Color LineColor { get; set; } = Color.FromArgb("#1565C0");
    public Color AxisColor { get; set; } = Color.FromArgb("#B0BEC5");
    public Color LabelColor { get; set; } = Color.FromArgb("#78909C");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count == 0) return;

        const float padding = 24f;
        const float labelWidth = 44f;
        var left = padding + labelWidth;
        var width = dirtyRect.Width - left - padding;
        var height = dirtyRect.Height - padding * 2;
        if (width <= 0 || height <= 0) return;

        var max = Values.Max();
        var min = Math.Min(0, Values.Min());
        var range = max - min;
        if (range <= 0) range = 1;

        var baseline = dirtyRect.Height - padding;

        canvas.StrokeColor = AxisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, baseline, dirtyRect.Width - padding, baseline);
        canvas.DrawLine(left, padding, left, baseline);

        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 10;
        canvas.FontColor = LabelColor;
        canvas.DrawString(max.ToString(ValueFormat), 0, padding - 7, labelWidth + padding - 4, 14,
            HorizontalAlignment.Right, VerticalAlignment.Center);
        canvas.DrawString(min.ToString(ValueFormat), 0, baseline - 7, labelWidth + padding - 4, 14,
            HorizontalAlignment.Right, VerticalAlignment.Center);

        float Y(double value) => padding + height - (float)((value - min) / range * height);

        if (Values.Count == 1)
        {
            canvas.FillColor = LineColor;
            canvas.FillCircle(left + width / 2, Y(Values[0]), 4);
            return;
        }

        var stepX = width / (Values.Count - 1);
        var path = new PathF();
        for (var i = 0; i < Values.Count; i++)
        {
            var x = left + i * stepX;
            var y = Y(Values[i]);
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);

        if (Values.Count > 40) return;

        canvas.FillColor = LineColor;
        for (var i = 0; i < Values.Count; i++)
        {
            canvas.FillCircle(left + i * stepX, Y(Values[i]), 3.5f);
        }
    }
}
