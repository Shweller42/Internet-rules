using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Models;

namespace WpfApp1;

public partial class TheoryPage : Page
{
    private INavigationService _nav;
    private Topic _topic;

    public TheoryPage(INavigationService nav, Topic topic)
    {
        InitializeComponent();
        _nav = nav;
        _topic = topic;

        TopicIcon.Text = topic.Icon;
        TopicTitle.Text = topic.Title;
        TopicDesc.Text = topic.Description;

        var converter = new BrushConverter();
        var rgba = (Brush)converter.ConvertFromString("#FFF7ED")!;
        var orange = (Brush)converter.ConvertFromString("#C05621")!;
        var brown = (Brush)converter.ConvertFromString("#9A3412")!;
        var gray = (Brush)converter.ConvertFromString("#718096")!;

        for (int i = 0; i < topic.Rules.Count; i++)
        {
            var r = topic.Rules[i];
            var sourceLabel = !string.IsNullOrEmpty(r.Source) ? $"📚 Источник: {r.Source}" : "";
            var primaryBg = (Brush)FindResource("PrimaryLightBrush");
            var primaryFg = (Brush)FindResource("PrimaryBrush");
            var surfaceBg = (Brush)FindResource("SurfaceBrush");
            var borderBr = (Brush)FindResource("BorderBrush");

            RulesPanel.Children.Add(new Border
            {
                Background = surfaceBg,
                CornerRadius = new CornerRadius(16),
                BorderBrush = borderBr,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(28),
                Margin = new Thickness(0, 0, 0, 20),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Правило {i + 1} из {topic.Rules.Count}",
                            FontSize = 12,
                            Foreground = primaryFg,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 12)
                        },
                        new Border
                        {
                            Background = primaryBg,
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(24),
                            Margin = new Thickness(0, 0, 0, 16),
                            Child = new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "📌 Правило",
                                        FontSize = 12,
                                        Foreground = primaryFg,
                                        FontWeight = FontWeights.SemiBold,
                                        Margin = new Thickness(0, 0, 0, 8)
                                    },
                                    new TextBlock
                                    {
                                        Text = r.Text,
                                        FontSize = 17,
                                        FontWeight = FontWeights.Medium,
                                        Foreground = primaryFg,
                                        TextWrapping = TextWrapping.Wrap,
                                        LineHeight = 26
                                    }
                                }
                            }
                        },
                        new Border
                        {
                            Background = rgba,
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(24),
                            Margin = new Thickness(0, 0, 0, 12),
                            Child = new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "💡 Пример",
                                        FontSize = 12,
                                        Foreground = orange,
                                        FontWeight = FontWeights.SemiBold,
                                        Margin = new Thickness(0, 0, 0, 8)
                                    },
                                    new TextBlock
                                    {
                                        Text = r.Example,
                                        FontSize = 14,
                                        Foreground = brown,
                                        TextWrapping = TextWrapping.Wrap,
                                        FontStyle = FontStyles.Italic,
                                        LineHeight = 22
                                    }
                                }
                            }
                        },
                        new TextBlock
                        {
                            Text = sourceLabel,
                            FontSize = 11,
                            Foreground = gray,
                            TextWrapping = TextWrapping.Wrap,
                            FontStyle = FontStyles.Italic
                        }
                    }
                }
            });
        }
    }

    private void GoToTest(object sender, RoutedEventArgs e) => _nav.ShowTest(_topic);
    // Unused: sort/case are chained after test via TestSession
}