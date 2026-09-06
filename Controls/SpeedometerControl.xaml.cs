using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1.Controls;

public partial class SpeedometerControl : UserControl
{
    public static readonly DependencyProperty PercentageProperty =
        DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(SpeedometerControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Percentage
    {
        get => (double)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public SpeedometerControl()
    {
        InitializeComponent();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        double cx = w / 2;
        double cy = h - 6;
        double radius = Math.Min(w / 2, h) - 10;
        if (radius < 10) return;

        double pct = Math.Clamp(Percentage, 0, 100);
        double progAngle = -180 + (pct / 100.0) * 180;
        double progRad = progAngle * Math.PI / 180;

        double step = 3;
        for (double a = -180; a < 0; a += step)
        {
            double aRad = a * Math.PI / 180;
            double aRadNext = (a + step) * Math.PI / 180;
            double x1 = cx + radius * Math.Cos(aRad);
            double y1 = cy + radius * Math.Sin(aRad);
            double x2 = cx + radius * Math.Cos(aRadNext);
            double y2 = cy + radius * Math.Sin(aRadNext);

            bool isActive = a + step <= progAngle;
            Color color;
            if (isActive)
            {
                double midPct = (a + 180 + step / 2) / 180.0 * 100;
                color = midPct < 40 ? Color.FromRgb(252, 129, 129)
                      : midPct < 65 ? Color.FromRgb(246, 224, 94)
                      : Color.FromRgb(72, 187, 120);
            }
            else
            {
                color = Color.FromRgb(226, 232, 240);
            }

            dc.DrawLine(
                new Pen(new SolidColorBrush(color), 5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                new Point(x1, y1), new Point(x2, y2));
        }

        // Needle
        double needleLen = radius - 3;
        double nx = cx + needleLen * Math.Cos(progRad);
        double ny = cy + needleLen * Math.Sin(progRad);
        dc.DrawLine(new Pen(Brushes.DimGray, 2.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
            new Point(cx, cy), new Point(nx, ny));

        // Center dot
        dc.DrawEllipse(Brushes.DimGray, null, new Point(cx, cy), 3.5, 3.5);

        // Percentage text
        var ft = new FormattedText(
            $"{pct:F0}%",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            Brushes.DimGray,
            1.0);
        ft.TextAlignment = TextAlignment.Center;
        dc.DrawText(ft, new Point(cx - 25, cy - radius - 14));
    }
}
