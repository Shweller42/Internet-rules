using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1.Models;

namespace WpfApp1;

public partial class CasePage : Page
{
    private static Brush HexBrush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    private static readonly Brush DefaultBg = HexBrush("#FFFFFF");
    private static readonly Brush DefaultBorder = HexBrush("#E2E8F0");
    private static readonly Brush SelectedBg = HexBrush("#EBF4FF");
    private static readonly Brush SelectedBorder = HexBrush("#4C51BF");

    private INavigationService _nav;
    private Topic _topic;
    private TestSession _session;
    private CaseOptionVm? _selectedOption;
    private Button? _selectedButton;
    private bool _finished;

    public CasePage(INavigationService nav, Topic topic, TestSession session)
    {
        InitializeComponent();
        _nav = nav;
        _topic = topic;
        _session = session;
        _session.CurrentStage = TestStage.Case;
        ShowCase();
    }

    private void ShowCase()
    {
        if (_topic.Case == null) return;
        SituationText.Text = _topic.Case.Description;
        OptionsPanel.ItemsSource = _topic.Case.Options.Select((opt, i) => new CaseOptionVm
        {
            Text = opt.Text,
            Index = i,
            CaseOption = opt
        }).ToList();
        StageInfo.Text = "Финальное задание";
        ExplanationBox.Visibility = Visibility.Collapsed;
        FinishCaseBtn.Visibility = Visibility.Collapsed;
    }

    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (_finished) return;

        var btn = (Button)sender;
        _selectedOption = (CaseOptionVm)btn.DataContext;

        if (_selectedButton != null)
        {
            _selectedButton.Background = DefaultBg;
            _selectedButton.BorderBrush = DefaultBorder;
        }
        btn.Background = SelectedBg;
        btn.BorderBrush = SelectedBorder;
        _selectedButton = btn;

        _finished = true;

        var isCorrect = _selectedOption.CaseOption.IsCorrect;
        _session.CaseCorrect = isCorrect;

        if (isCorrect)
        {
            ExplanationBox.Background = HexBrush("#F0FFF4");
            ExplanationTitle.Text = "✓ Верно!";
            ExplanationTitle.Foreground = HexBrush("#276749");
            ExplanationText.Text = _selectedOption.CaseOption.Explanation;
            ExplanationText.Foreground = HexBrush("#276749");
        }
        else
        {
            ExplanationBox.Background = HexBrush("#FFF5F5");
            ExplanationTitle.Text = "✗ Неверно";
            ExplanationTitle.Foreground = HexBrush("#C53030");
            ExplanationText.Text = _selectedOption.CaseOption.Explanation;
            ExplanationText.Foreground = HexBrush("#9B2C2C");
        }

        ExplanationBox.Visibility = Visibility.Visible;
        FinishCaseBtn.Visibility = Visibility.Visible;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            FinishCaseBtn.BringIntoView();
        }));
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        var result = _session.GetCombinedResult();
        SaveResult(result);
        _nav.ShowResult(result);
    }

    private void SaveResult(TestResult result)
    {
        DatabaseService.SaveProgress(result.TopicName, result.Percentage, result.Grade);
    }
}

public class CaseOptionVm
{
    public string Text { get; set; } = "";
    public int Index { get; set; }
    public CaseOption CaseOption { get; set; } = new();
}
