using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfApp1.Models;

namespace WpfApp1;

public partial class ResultPage : Page
{
    private INavigationService _nav;
    private TestResult _result;
    private List<Theme> _allThemes;

    public ResultPage(INavigationService nav, TestResult result, List<Theme> allThemes)
    {
        InitializeComponent();
        _nav = nav;
        _result = result;
        _allThemes = allThemes;

        ThemeLabel.Text = $"Результат: {result.TopicName}";
        PercentText.Text = $"{result.Percentage:F0}%";
        ScoreText.Text = $"Правильных ответов: {result.CorrectCount} из {result.TotalCount}";

        if (result.Percentage >= 80)
        {
            GradeText.Text = "Отлично";
            GradeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48BB78"));
        }
        else if (result.Percentage >= 60)
        {
            GradeText.Text = "Хорошо";
            GradeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14B8A6"));
        }
        else
        {
            GradeText.Text = "Плохо";
            GradeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FC8181"));
        }

        if (result.AllAnswers.Count > 0)
        {
            AnswerSection.Visibility = Visibility.Visible;
            AnswersList.ItemsSource = result.AllAnswers.Select(a => new AnswerVm
            {
                Question = a.Question,
                UserAnswer = a.UserAnswer,
                CorrectAnswer = a.CorrectAnswer,
                Background = a.IsCorrect ? "#F0FFF4" : "#FFF5F5",
                TextColor = a.IsCorrect ? "#276749" : "#C53030"
            }).ToList();
            int totalErrors = result.TotalCount - result.CorrectCount;
            ErrorCountText.Text = $"Ошибок: {totalErrors}";
        }
    }

    private List<ProgressRecord> LoadProgress()
    {
        var allTopics = _allThemes.SelectMany(ch => ch.Topics).ToList();
        var dbProgress = DatabaseService.LoadProgress();
        var list = new List<ProgressRecord>();

        foreach (var t in allTopics)
        {
            var match = dbProgress.FirstOrDefault(p => p.TopicName == t.Title);
            list.Add(new ProgressRecord
            {
                ThemeName = t.Title,
                TopicName = t.Title,
                Percentage = match?.Percentage ?? 0
            });
        }

        return list;
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        var topic = _allThemes
            .SelectMany(ch => ch.Topics)
            .First(t => t.Title == _result.TopicName);
        _nav.ShowTheory(topic);
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        _nav.GoToMainPage();
    }
}

public class AnswerVm
{
    public string Question { get; set; } = "";
    public string UserAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string Background { get; set; } = "#F0FFF4";
    public string TextColor { get; set; } = "#276749";
}
