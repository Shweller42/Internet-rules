using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Models;

namespace WpfApp1;

public partial class TopicsPage : Page
{
    private INavigationService _nav;
    private Theme _chapter;
    private List<TopicVm> _topicVms;

    public TopicsPage(INavigationService nav, Theme chapter)
    {
        InitializeComponent();
        _nav = nav;
        _chapter = chapter;

        var progress = DatabaseService.LoadProgress();
        var activeSession = nav.CurrentSession;
        _topicVms = chapter.Topics.Select(t =>
        {
            var match = progress.FirstOrDefault(p => p.ThemeName == t.Title);
            double pct = match?.Percentage ?? 0;
            var hasSession = activeSession != null && activeSession.Topic.Id == t.Id
                             && activeSession.CurrentStage != TestStage.NotStarted;
            return new TopicVm
            {
                Id = t.Id,
                Icon = t.Icon,
                Title = t.Title,
                Description = t.Description,
                StatusText = $"{pct:F0}%",
                StatusForeground = pct >= 80 ? "#48BB78" : pct >= 50 ? "#D69E2E" : pct > 0 ? "#FC8181" : "#A0AEC0",
                HasActiveSession = hasSession
            };
        }).ToList();

        TopicsList.ItemsSource = _topicVms;
    }

    private TopicVm? FindVm(int id)
    {
        return _topicVms.FirstOrDefault(v => v.Id == id);
    }

    private void Topic_Click(object sender, MouseButtonEventArgs e)
    {
        var border = (FrameworkElement)sender;
        var vm = FindVm((int)border.Tag);
        if (vm != null) NavigateToTopic(vm);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        var btn = (FrameworkElement)sender;
        var vm = FindVm((int)btn.Tag);
        if (vm != null) NavigateToTopic(vm);
    }

    private void NavigateToTopic(TopicVm vm)
    {
        var topic = _chapter.Topics.First(t => t.Id == vm.Id);
        _nav.ShowTheory(topic);
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var btn = (FrameworkElement)sender;
        var vm = FindVm((int)btn.Tag);
        if (vm == null) return;

        var topic = _chapter.Topics.First(t => t.Id == vm.Id);
        var session = _nav.CurrentSession;
        if (session == null || session.Topic.Id != topic.Id) return;

        switch (session.CurrentStage)
        {
            case TestStage.Test:
                _nav.ShowTest(topic, session);
                break;
            case TestStage.Sort:
                _nav.ShowSort(topic, session);
                break;
            case TestStage.Case:
                _nav.ShowCase(topic, session);
                break;
        }
    }

}

public class TopicVm
{
    public int Id { get; set; }
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string StatusText { get; set; } = "Не пройдено";
    public string StatusForeground { get; set; } = "#A0AEC0";
    public bool HasActiveSession { get; set; }
}
