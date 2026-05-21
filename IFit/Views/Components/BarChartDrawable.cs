using Microsoft.Maui.Graphics;

namespace IFit.Views.Components
{
    public class BarChartDrawable : IDrawable
    {
        private readonly double[] _values;
        private readonly string[] _labels;

        public BarChartDrawable(double[] values, string[] labels)
        {
            _values = values;
            _labels = labels;
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (_values.Length == 0) return;

            const float xLabelH   = 22f;
            const float dataLabelH = 18f;
            const float topPad    = 4f;

            float usableH = rect.Height - xLabelH - dataLabelH - topPad;
            float usableW = rect.Width;

            double max = _values.Max();
            if (max == 0) max = 1;

            int n      = _values.Length;
            float slot = usableW / n;
            float barW = Math.Min(slot * 0.55f, 36f);

            var barColor = Color.FromArgb("#FFD369");
            var valColor = Color.FromArgb("#EEEEEE");
            var lblColor = Color.FromArgb("#99EEEEEE");

            canvas.Antialias = true;

            for (int i = 0; i < n; i++)
            {
                float cx   = slot * i + slot / 2f;
                float barH = (float)(_values[i] / max * usableH);
                float barX = cx - barW / 2f;
                float barY = topPad + dataLabelH + (usableH - barH);

                canvas.FillColor = barColor;
                canvas.FillRoundedRectangle(barX, barY, barW, barH, 4);

                canvas.FontColor = valColor;
                canvas.FontSize  = 11;
                canvas.DrawString(
                    ((int)_values[i]).ToString(),
                    cx - 20, barY - dataLabelH, 40, dataLabelH,
                    HorizontalAlignment.Center, VerticalAlignment.Center);

                canvas.FontColor = lblColor;
                canvas.FontSize  = 10;
                canvas.DrawString(
                    _labels.Length > i ? _labels[i] : string.Empty,
                    cx - slot * 0.6f, rect.Height - xLabelH, slot * 1.2f, xLabelH,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}
