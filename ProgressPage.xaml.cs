using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfApp1.Models;

namespace WpfApp1;

public partial class ProgressPage : Page
{
    private static Brush HexBrush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);

    private readonly INavigationService _nav;
    private readonly List<Theme> _themes;

    public ProgressPage(INavigationService nav, List<Theme> themes)
    {
        InitializeComponent();
        _nav = nav;
        _themes = themes;
        BuildStats();
        BuildPieChart();
        BuildBarChart();
        BuildErrorAnalysis();
    }

    private List<(string Label, string Value)> ComputeStats()
    {
        var allTopics = _themes.SelectMany(t => t.Topics).ToList();
        var progress = DatabaseService.LoadProgress();

        var topicPcts = allTopics.Select(t =>
        {
            var p = progress.FirstOrDefault(p => p.TopicName == t.Title);
            return p?.Percentage ?? 0;
        }).ToList();

        int total = topicPcts.Count;
        int passed = topicPcts.Count(p => p >= 80);
        int medium = topicPcts.Count(p => p >= 60 && p < 80);
        int poor = topicPcts.Count(p => p > 0 && p < 60);
        int attempted = topicPcts.Count(p => p > 0);
        double avg = attempted > 0 ? topicPcts.Where(p => p > 0).Average() : 0;
        double best = attempted > 0 ? topicPcts.Max() : 0;
        double worst = attempted > 0 ? topicPcts.Where(p => p > 0).Min() : 0;

        return new()
        {
            ("Всего тем", total.ToString()),
            ("Пройдено на отлично", passed.ToString()),
            ("Пройдено средне", medium.ToString()),
            ("Пройдено на плохо", poor.ToString()),
            ("Начато", attempted.ToString()),
            ("Средний балл", $"{avg:F1}%"),
            ("Лучший результат", $"{best:F0}%"),
            ("Худший результат", worst > 0 ? $"{worst:F0}%" : "—"),
        };
    }

    private void BuildStats()
    {
        StatsCards.ItemsSource = ComputeStats().Select(s => new { s.Label, s.Value }).ToList();
    }

    private void BuildPieChart()
    {
        var allTopics = _themes.SelectMany(t => t.Topics).ToList();
        var progress = DatabaseService.LoadProgress();

        int passed = 0, inProgress = 0, notStarted = 0;
        foreach (var t in allTopics)
        {
            var p = progress.FirstOrDefault(p => p.TopicName == t.Title);
            double pct = p?.Percentage ?? 0;
            if (pct >= 80) passed++;
            else if (pct > 0) inProgress++;
            else notStarted++;
        }

        double totalW = 220, totalH = 220;
        double cx = totalW / 2, cy = totalH / 2, r = 90;

        var segments = new[]
        {
            (Value: passed, Color: "#48BB78", Label: $"Пройдено ({passed})"),
            (Value: inProgress, Color: "#D69E2E", Label: $"В процессе ({inProgress})"),
            (Value: notStarted, Color: "#E2E8F0", Label: $"Не начато ({notStarted})"),
        };

        int total = passed + inProgress + notStarted;
        double curAngle = 0;

        foreach (var seg in segments)
        {
            if (seg.Value == 0) continue;
            double sweep = seg.Value / (double)total * 360.0;
            if (total == 0) sweep = 360;

            double a1 = curAngle - 90;
            double a2 = a1 + sweep;
            double r1 = a1 * Math.PI / 180, r2 = a2 * Math.PI / 180;

            double x1 = cx + r * Math.Cos(r1);
            double y1 = cy + r * Math.Sin(r1);
            double x2 = cx + r * Math.Cos(r2);
            double y2 = cy + r * Math.Sin(r2);

            bool large = sweep > 180;
            var path = new Path
            {
                Fill = HexBrush(seg.Color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Data = new PathGeometry
                {
                    Figures =
                    {
                        new PathFigure
                        {
                            StartPoint = new Point(cx, cy),
                            Segments =
                            {
                                new LineSegment(new Point(x1, y1), true),
                                new ArcSegment(new Point(x2, y2), new Size(r, r), 0, large, SweepDirection.Clockwise, true),
                            },
                            IsClosed = true,
                        }
                    }
                }
            };

            PieCanvas.Children.Add(path);
            curAngle += sweep;
        }

        // Center hole for donut effect
        var hole = new Ellipse { Width = 70, Height = 70, Fill = Brushes.White };
        Canvas.SetLeft(hole, cx - 35);
        Canvas.SetTop(hole, cy - 35);
        PieCanvas.Children.Add(hole);

        // Center text
        var centerText = new TextBlock
        {
            Text = $"{total}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.DimGray,
        };
        Canvas.SetLeft(centerText, cx - 12);
        Canvas.SetTop(centerText, cy - 13);
        PieCanvas.Children.Add(centerText);

        // Legend
        PieLegend.ItemsSource = segments.Where(s => s.Value > 0).Select(s => new
        {
            Color = s.Color,
            Label = s.Label
        }).ToList();
    }

    private static string ShortenLabel(string title, int maxLen)
    {
        if (title.Length <= maxLen) return title;
        var words = title.Split(' ');
        if (words.Length > 1)
        {
            string first = words[0];
            if (first.Length <= maxLen - 1) return first + "…";
        }
        return title[..(maxLen - 1)] + "…";
    }

    private void BuildBarChart()
    {
        var allTopics = _themes.SelectMany(t => t.Topics).ToList();
        var progress = DatabaseService.LoadProgress();

        var canvas = BarCanvas;
        double totalW = canvas.ActualWidth;
        if (totalW <= 0) totalW = 680;
        double h = allTopics.Count * 36.0 + 20;
        canvas.Height = h;

        double labelW = 140;
        double barAreaW = totalW - labelW - 60;
        if (barAreaW < 80) barAreaW = 80;
        double maxPct = 100;

        for (int i = 0; i < allTopics.Count; i++)
        {
            var p = progress.FirstOrDefault(p => p.TopicName == allTopics[i].Title);
            double pct = p?.Percentage ?? 0;
            double barW = (pct / maxPct) * barAreaW;
            double y = 10 + i * 36;

            var color = pct >= 80 ? "#48BB78" : pct >= 50 ? "#D69E2E" : pct > 0 ? "#FC8181" : "#E2E8F0";

            var label = new TextBlock
            {
                Text = ShortenLabel(allTopics[i].Title, 14),
                FontSize = 11,
                Foreground = HexBrush("#4A5568"),
                TextWrapping = TextWrapping.Wrap,
                Width = labelW - 8,
                ToolTip = allTopics[i].Title,
            };
            Canvas.SetLeft(label, 4);
            Canvas.SetTop(label, y + 4);
            canvas.Children.Add(label);

            var rect = new Rectangle
            {
                Width = barW > 0 ? barW : 2,
                Height = 22,
                Fill = HexBrush(color),
                RadiusX = 4, RadiusY = 4,
                ToolTip = $"{allTopics[i].Title}: {pct:F0}%"
            };
            Canvas.SetLeft(rect, labelW);
            Canvas.SetTop(rect, y + 4);
            canvas.Children.Add(rect);

            if (pct > 0)
            {
                var tb = new TextBlock
                {
                    Text = $"{pct:F0}%",
                    FontSize = 10,
                    Foreground = HexBrush("#2D3748"),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Canvas.SetLeft(tb, labelW + barW + 6);
                Canvas.SetTop(tb, y + 5);
                canvas.Children.Add(tb);
            }
        }
    }

    private void BuildErrorAnalysis()
    {
        var dbErrors = LoadAllTestAnswers();
        if (dbErrors.Count == 0) return;

        ErrorsSection.Visibility = Visibility.Visible;

        var byTopic = dbErrors.GroupBy(e => e.TopicName)
            .Select(g => new { Topic = g.Key, Errors = g.Count(), Correct = g.Count(e => e.IsCorrect) })
            .ToList();

        var least = byTopic.OrderBy(b => b.Errors).First();
        var most = byTopic.OrderByDescending(b => b.Errors).First();

        LeastErrorsText.Text = $"✅ Меньше всего ошибок: «{least.Topic}» — {least.Errors} ошибок";
        MostErrorsText.Text = $"❌ Больше всего ошибок: «{most.Topic}» — {most.Errors} ошибок";
    }

    private List<ErrorRecord> LoadAllTestAnswers()
    {
        var list = new List<ErrorRecord>();
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "progress.db")}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TopicName, QuestionText, UserAnswer, CorrectAnswer FROM TestAnswers";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ErrorRecord
            {
                TopicName = reader.GetString(0),
                IsCorrect = reader.GetString(2) == reader.GetString(3)
            });
        }
        return list;
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        _nav.GoToMainPage();
    }
}

public class ErrorRecord
{
    public string TopicName { get; set; } = "";
    public bool IsCorrect { get; set; }
}

