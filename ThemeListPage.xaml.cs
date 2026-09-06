using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;

namespace WpfApp1;

public partial class ThemeListPage : Page
{
    private INavigationService _nav;
    private List<Theme> _themes;

    public ThemeListPage(INavigationService nav, List<Theme> themes)
    {
        InitializeComponent();
        _nav = nav;
        _themes = themes;
        LoadThemes();
        UpdateProgress();
    }

    private void LoadThemes()
    {
        var progress = DatabaseService.LoadProgress();
        var items = _themes.Select(t =>
        {
            var topicPcts = t.Topics
                .Select(topic =>
                {
                    var p = progress.FirstOrDefault(p => p.TopicName == topic.Title);
                    return p?.Percentage ?? 0;
                }).ToList();
            var completed = topicPcts.Count(p => p > 0);
            var avg = topicPcts.Count > 0 ? topicPcts.Average() : 0;
            return new
            {
                t.Id,
                t.Icon,
                t.Title,
                t.Description,
                ProgressPct = avg,
                ProgressLabel = completed > 0 ? $"Пройдено: {completed}/{topicPcts.Count}" : "",
                HasProgress = completed > 0
            };
        }).ToList();

        ThemesList.ItemsSource = items;
    }

    private void UpdateProgress()
    {
        var progress = DatabaseService.LoadProgress();
        var distinct = progress.Count(p => p.Percentage > 0);
        ProgressText.Text = distinct > 0
            ? $"Пройдено тем: {distinct}"
            : "Пока нет пройденных тем";
    }

    private void Progress_Click(object sender, RoutedEventArgs e)
    {
        _nav.ShowProgress();
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        var btn = (FrameworkElement)sender;
        int id = (int)btn.Tag;
        var chapter = _themes.Find(t => t.Id == id);
        if (chapter != null)
            _nav.ShowTopics(chapter);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog();
        dialog.ShowDialog();
        if (dialog.Confirmed)
        {
            DatabaseService.ResetAll();
            ProgressText.Text = "Пока нет пройденных тем";
            LoadThemes();
        }
    }
}
