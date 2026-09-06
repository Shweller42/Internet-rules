using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;

namespace WpfApp1;

public partial class TestPage : Page
{
    private INavigationService _nav;
    private Topic _topic;
    private TestSession _session;
    private int _qIndex;

    private static readonly Random _rng = new();

    public TestPage(INavigationService nav, Topic topic, TestSession session)
    {
        InitializeComponent();
        _nav = nav;
        _topic = topic;
        _session = session;
        _session.CurrentStage = TestStage.Test;

        if (_session.CurrentQuestionIndex == 0)
            ShuffleQuestions();

        _qIndex = _session.CurrentQuestionIndex;
        ShowQuestion();
    }

    private void ShuffleQuestions()
    {
        var rng = _rng;
        var questions = _topic.Questions;

        // Shuffle question order
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (questions[i], questions[j]) = (questions[j], questions[i]);
        }

        // Shuffle options within each question
        foreach (var q in questions)
        {
            var options = q.Options.ToList();
            int correct = q.CorrectIndex;
            string correctText = options[correct];

            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            q.CorrectIndex = options.IndexOf(correctText);
            q.Options = options;
        }
    }

    private void ShowQuestion()
    {
        var q = _topic.Questions[_qIndex];
        QuestionText.Text = q.Text;
        OptionsList.ItemsSource = q.Options.Select((text, i) => new OptionItem { Text = text, Index = i }).ToList();
        ProgressInfo.Text = $"Вопрос {_qIndex + 1} из {_topic.Questions.Count}";
        NextQuestionBtn.IsEnabled = false;
        FinishBtn.IsEnabled = false;
        bool isLast = _qIndex >= _topic.Questions.Count - 1;
        NextQuestionBtn.Visibility = isLast ? Visibility.Collapsed : Visibility.Visible;
        FinishBtn.Visibility = isLast ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        var rb = (System.Windows.Controls.RadioButton)sender;
        var opt = (OptionItem)rb.DataContext;
        _topic.Questions[_qIndex].UserAnswer = opt.Index;
        bool isLast = _qIndex >= _topic.Questions.Count - 1;
        NextQuestionBtn.IsEnabled = !isLast;
        FinishBtn.IsEnabled = isLast;
    }

    private void NextQuestion_Click(object sender, RoutedEventArgs e)
    {
        _session.CurrentQuestionIndex = _qIndex + 1;
        _qIndex++;
        ShowQuestion();
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        var questions = _topic.Questions;
        _session.TestCorrect = questions.Count(q => q.UserAnswer == q.CorrectIndex);
        _session.TestTotal = questions.Count;
        _session.TestErrors = questions
            .Where(q => q.UserAnswer != q.CorrectIndex && q.UserAnswer >= 0)
            .Select(q => new ResultError
            {
                Question = q.Text,
                UserAnswer = q.Options[q.UserAnswer],
                CorrectAnswer = q.Options[q.CorrectIndex]
            }).ToList();
        _session.AllAnswers = questions
            .Select(q => new ResultError
            {
                Question = q.Text,
                UserAnswer = q.UserAnswer >= 0 ? q.Options[q.UserAnswer] : "(не выбран)",
                CorrectAnswer = q.Options[q.CorrectIndex],
                IsCorrect = q.UserAnswer == q.CorrectIndex
            }).ToList();

        if (_topic.SortCards.Count > 0)
            _nav.ShowSort(_topic, _session);
        else if (_topic.Case != null)
            _nav.ShowCase(_topic, _session);
        else
            FinishSession();
    }

    private void FinishSession()
    {
        var result = _session.GetCombinedResult();
        DatabaseService.SaveProgress(result.TopicName, result.Percentage, result.Grade);
        _nav.ShowResult(result);
    }
}

public class OptionItem
{
    public string Text { get; set; } = "";
    public int Index { get; set; }
}