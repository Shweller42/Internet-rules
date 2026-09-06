using System.Collections.Generic;

namespace WpfApp1.Models;

public class Theme
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<Topic> Topics { get; set; } = new();
}

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "📌";
    public string Description { get; set; } = "";
    public List<Rule> Rules { get; set; } = new();
    public List<TestQuestion> Questions { get; set; } = new();
    public List<SortCard> SortCards { get; set; } = new();
    public List<string> SortCategories { get; set; } = new();
    public CaseScenario? Case { get; set; }

    public double ProgressPct { get; set; }
    public bool HasProgress => ProgressPct > 0;
}

public class Rule
{
    public string Text { get; set; } = "";
    public string Example { get; set; } = "";
    public string Source { get; set; } = "";
}

public class TestQuestion
{
    public string Text { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public int UserAnswer { get; set; } = -1;
}

public class SortCard
{
    public string Text { get; set; } = "";
    public int CorrectCategoryIndex { get; set; }
    public int UserCategoryIndex { get; set; } = -1;
}

public class CaseScenario
{
    public string Description { get; set; } = "";
    public List<CaseOption> Options { get; set; } = new();
}

public class CaseOption
{
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = "";
}

public class TestResult
{
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public double Percentage { get; set; }
    public string Grade { get; set; } = "";
    public List<ResultError> Errors { get; set; } = new();
    public List<ResultError> AllAnswers { get; set; } = new();
    public string ThemeName { get; set; } = "";
    public string TopicName { get; set; } = "";
}

public class ResultError
{
    public string Question { get; set; } = "";
    public string UserAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public bool IsCorrect { get; set; }
}

public class ProgressRecord
{
    public string ThemeName { get; set; } = "";
    public string TopicName { get; set; } = "";
    public double Percentage { get; set; }
}

public class SortCardViewModel
{
    public string Text { get; }
    public int CategoryId { get; }

    public SortCardViewModel(SortCard card)
    {
        Text = card.Text;
        CategoryId = card.CorrectCategoryIndex;
    }
}

public class SortCategoryViewModel
{
    public string Name { get; }
    public int Index { get; }

    public SortCategoryViewModel(string name, int index)
    {
        Name = name;
        Index = index;
    }
}

public enum TestStage { NotStarted, Test, Sort, Case }

public class TestSession
{
    public Topic Topic { get; set; } = new();
    public TestStage CurrentStage { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public int TestCorrect { get; set; }
    public int TestTotal { get; set; }
    public List<ResultError> TestErrors { get; set; } = new();
    public int SortCorrect { get; set; }
    public int SortTotal { get; set; }
    public bool CaseCorrect { get; set; }
    public string CaseExplanation { get; set; } = "";
    public List<ResultError> AllAnswers { get; set; } = new();
    public List<SortCardViewModel>? SortCards { get; set; }
    public List<SortCategoryViewModel>? SortCategories { get; set; }

    public TestResult GetCombinedResult()
    {
        int total = TestTotal + SortTotal + 1;
        int correct = TestCorrect + SortCorrect + (CaseCorrect ? 1 : 0);
        double pct = total > 0 ? (double)correct / total * 100 : 0;
        string grade = pct >= 80 ? "Отлично" : pct >= 60 ? "Хорошо" : "Плохо";

        var errors = new List<ResultError>(TestErrors);
        return new TestResult
        {
            CorrectCount = correct,
            TotalCount = total,
            Percentage = pct,
            Grade = grade,
            Errors = errors,
            AllAnswers = AllAnswers,
            ThemeName = Topic.Title,
            TopicName = Topic.Title
        };
    }
}

public interface INavigationService
{
    TestSession? CurrentSession { get; }
    void ShowTopics(Theme chapter);
    void ShowTheory(Topic topic);
    void ShowTest(Topic topic, TestSession? session = null);
    void ShowSort(Topic topic, TestSession session);
    void ShowCase(Topic topic, TestSession session);
    void ShowResult(TestResult result);
    void ShowProgress();
    void GoToMainPage();
}
