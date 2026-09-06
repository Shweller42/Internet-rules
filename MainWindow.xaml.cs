using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using WpfApp1.Models;

namespace WpfApp1;

public partial class MainWindow : Window, INavigationService
{
    private List<Theme> _chapters;
    private Theme? _currentChapter;
    private Topic? _currentTopic;
    private bool _isAtChapterLevel;
    internal TestSession? _currentSession;

    public MainWindow()
    {
        InitializeComponent();

        var workArea = SystemParameters.WorkArea;
        double w = workArea.Width * 0.55;
        double h = workArea.Height * 0.82;
        Width = w < 920 ? 920 : w > 1100 ? 1000 : w;
        Height = h < 660 ? 660 : h > 800 ? 760 : h;

        DatabaseService.Initialize();
        _chapters = ContentLoader.Load();

        ContentFrame.Navigating += ContentFrame_Navigating;
        ContentFrame.CommandBindings.Add(new CommandBinding(
            NavigationCommands.BrowseBack,
            (s, e) => e.Handled = true));

        ShowMainPage();
    }

    private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
            e.Cancel = true;
    }

    private void NavigateTo(Page page)
    {
        ContentFrame.Opacity = 0;
        ContentFrame.Content = page;
        var fade = new DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(180));
        fade.EasingFunction = new QuadraticEase();
        ContentFrame.BeginAnimation(OpacityProperty, fade);
    }

    private void ShowMainPage()
    {
        _isAtChapterLevel = false;
        _currentChapter = null;
        _currentTopic = null;
        MainHeaderContent.Visibility = Visibility.Visible;
        ThemeHeaderContent.Visibility = Visibility.Collapsed;
        BackBtn.Visibility = Visibility.Collapsed;
        NavigateTo(new ThemeListPage(this, _chapters));
    }

    public void GoToMainPage()
    {
        ShowMainPage();
    }

    public void ShowTopics(Theme chapter)
    {
        _currentChapter = chapter;
        _currentTopic = null;
        _isAtChapterLevel = true;
        ThemeIconText.Text = chapter.Icon;
        ThemeTitleText.Text = chapter.Title;
        MainHeaderContent.Visibility = Visibility.Collapsed;
        ThemeHeaderContent.Visibility = Visibility.Visible;
        BackBtn.Visibility = Visibility.Visible;
        NavigateTo(new TopicsPage(this, chapter));
    }

    public void ShowTheory(Topic topic)
    {
        _currentTopic = topic;
        _isAtChapterLevel = false;
        _currentSession = null;
        NavigateTo(new TheoryPage(this, topic));
    }

    public void ShowTest(Topic topic, TestSession? session = null)
    {
        _currentTopic = topic;
        _isAtChapterLevel = false;
        _currentSession = session ?? new TestSession { Topic = topic };
        NavigateTo(new TestPage(this, topic, _currentSession));
    }

    public void ShowSort(Topic topic, TestSession session)
    {
        _currentTopic = topic;
        _isAtChapterLevel = false;
        _currentSession = session;
        NavigateTo(new SortPage(this, topic, session));
    }

    public void ShowCase(Topic topic, TestSession session)
    {
        _currentTopic = topic;
        _isAtChapterLevel = false;
        _currentSession = session;
        NavigateTo(new CasePage(this, topic, session));
    }

    public void ShowProgress()
    {
        _isAtChapterLevel = false;
        _currentChapter = null;
        _currentTopic = null;
        MainHeaderContent.Visibility = Visibility.Visible;
        ThemeHeaderContent.Visibility = Visibility.Collapsed;
        BackBtn.Visibility = Visibility.Visible;
        NavigateTo(new ProgressPage(this, _chapters));
    }

    public void ShowResult(TestResult result)
    {
        _currentSession = null;
        _isAtChapterLevel = false;
        ThemeIconText.Text = "📊";
        ThemeTitleText.Text = result.ThemeName;
        MainHeaderContent.Visibility = Visibility.Collapsed;
        ThemeHeaderContent.Visibility = Visibility.Visible;
        BackBtn.Visibility = Visibility.Visible;
        NavigateTo(new ResultPage(this, result, _chapters));
    }

    TestSession? INavigationService.CurrentSession => _currentSession;

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        NavigateBack();
    }

    private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1)
        {
            e.Handled = true;
            NavigateBack();
        }
    }

    private void NavigateBack()
    {
        if (_isAtChapterLevel || _currentChapter == null)
            GoToMainPage();
        else
            ShowTopics(_currentChapter);
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}