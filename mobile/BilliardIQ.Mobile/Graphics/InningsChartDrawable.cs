namespace BilliardIQ.Mobile.Graphics;

public sealed class InningsChartDrawable : IDrawable
{
    public IReadOnlyList<float> Values { get; set; } = [];
    public Color LineColor { get; set; } = Color.FromArgb("#1565C0");
    public Color AxisColor { get; set; } = Color.FromArgb("#B0BEC5");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count == 0) return;

        const float padding = 24f;
        var width = dirtyRect.Width - padding * 2;
        var height = dirtyRect.Height - padding * 2;
        if (width <= 0 || height <= 0) return;

        var max = Math.Max(1f, Values.Max());

        canvas.StrokeColor = AxisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(padding, dirtyRect.Height - padding, dirtyRect.Width - padding, dirtyRect.Height - padding);

        if (Values.Count == 1)
        {
            var y = padding + height - (Values[0] / max * height);
            canvas.FillColor = LineColor;
            canvas.FillCircle(dirtyRect.Width / 2, y, 4);
            return;
        }

        var stepX = width / (Values.Count - 1);
        var path = new PathF();
        for (var i = 0; i < Values.Count; i++)
        {
            var x = padding + i * stepX;
            var y = padding + height - (Values[i] / max * height);
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 3;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);

        canvas.FillColor = LineColor;
        for (var i = 0; i < Values.Count; i++)
        {
            var x = padding + i * stepX;
            var y = padding + height - (Values[i] / max * height);
            canvas.FillCircle(x, y, 4);
        }
    }
}
