using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using WpfApp1.Models;

namespace WpfApp1;

public partial class SortPage : Page
{
    private readonly INavigationService _nav;
    private readonly List<SortCardViewModel> _cards = [];
    private readonly List<SortCategoryViewModel> _categories = [];
    private readonly Dictionary<SortCategoryViewModel, Border> _catBorders = [];
    private readonly Dictionary<SortCardViewModel, Border> _cardBorders = [];
    private readonly Dictionary<SortCategoryViewModel, WrapPanel> _catPanels = [];
    private readonly Dictionary<SortCardViewModel, bool> _placed = [];
    private readonly Dictionary<SortCardViewModel, SortCategoryViewModel?> _cardCategory = [];

    private Border? _dragBorder;
    private SortCardViewModel? _draggedCard;
    private Border? _ghost;
    private Point _ghostCanvasPos;
    private Point _dragStartMouse;
    private bool _isDragging;
    private bool _cardWasInCategory;
    private SortCategoryViewModel? _dragSourceCategory;

    private static Brush HexBrush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    private static Brush CloneBrush(Brush b) => b is Freezable f ? (Brush)f.CloneCurrentValue() : b;

    public SortPage(INavigationService nav, Topic topic, TestSession session)
    {
        InitializeComponent();
        _nav = nav;
        session.CurrentStage = TestStage.Sort;

        if (session.Topic.SortCards == null || session.Topic.SortCards.Count == 0
            || session.Topic.SortCategories == null || session.Topic.SortCategories.Count == 0)
        {
            HelpText.Text = "Нет данных для сортировки";
            return;
        }

        int idx = 0;
        foreach (var c in session.Topic.SortCategories)
            _categories.Add(new SortCategoryViewModel(c, idx++));
        foreach (var sc in session.Topic.SortCards)
        {
            var vm = new SortCardViewModel(sc);
            _cards.Add(vm);
            _placed[vm] = false;
            _cardCategory[vm] = null;
        }

        HelpText.Text = "Распределите карточки по категориям";
        BuildCards();
        BuildCategories();
    }

    private void BuildCards()
    {
        foreach (var vm in _cards)
        {
            var border = new Border
            {
                MaxWidth = 240, MinHeight = 56, Margin = new Thickness(4),
                Background = HexBrush("#EBF4FF"),
                BorderBrush = HexBrush("#667EEA"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12), Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = vm.Text, FontSize = 12, Foreground = HexBrush("#2D3748"),
                    TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(8), FontWeight = FontWeights.Medium,
                },
            };
            border.PreviewMouseLeftButtonDown += Card_PreviewMouseDown;
            CardsPanel.Children.Add(border);
            _cardBorders[vm] = border;
        }
    }

    private void BuildCategories()
    {
        foreach (var cat in _categories)
        {
            var cardsContainer = new WrapPanel();
            var sp = new StackPanel();
            sp.Children.Add(new Border
            {
                Background = HexBrush("#805AD5"), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = cat.Name, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, TextAlignment = TextAlignment.Center,
                },
            });
            sp.Children.Add(cardsContainer);

            var border = new Border
            {
                Width = 280, MinHeight = 120, Margin = new Thickness(6),
                Background = HexBrush("#FAF5FF"), BorderBrush = HexBrush("#D6BCFA"),
                BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12), Child = sp,
            };

            CategoriesPanel.Children.Add(border);
            _catBorders[cat] = border;
            _catPanels[cat] = cardsContainer;
        }
    }

    private void Card_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;

        var border = sender as Border;
        var vm = border != null ? _cardBorders.FirstOrDefault(kv => kv.Value == border).Key : null;
        if (border == null || vm == null) return;

        _isDragging = true;
        _dragBorder = border;
        _draggedCard = vm;

        var cardW = border.ActualWidth;
        var cardH = border.ActualHeight;
        var tb = (TextBlock)border.Child;

        _cardWasInCategory = _placed[vm];
        if (_cardWasInCategory)
        {
            _dragSourceCategory = _cardCategory[vm]!;
            _catPanels[_dragSourceCategory].Children.Remove(border);
            _placed[vm] = false;
            _cardCategory[vm] = null;
        }
        else
        {
            _dragSourceCategory = null;
            border.Visibility = Visibility.Collapsed;
        }

        _dragStartMouse = e.GetPosition(DragOverlay);
        _ghostCanvasPos = new Point(_dragStartMouse.X - cardW / 2, _dragStartMouse.Y - cardH / 2);
        _ghost = new Border
        {
            Width = cardW, Height = cardH,
            CornerRadius = border.CornerRadius,
            Background = CloneBrush(border.Background),
            BorderBrush = CloneBrush(border.BorderBrush),
            BorderThickness = border.BorderThickness,
            Effect = new DropShadowEffect
            {
                BlurRadius = 16, Opacity = 0.35, ShadowDepth = 4, Color = Colors.Black,
            },
            RenderTransformOrigin = new Point(0.5, 0.5),
            Child = new TextBlock
            {
                Text = tb.Text, FontSize = tb.FontSize,
                Foreground = CloneBrush(tb.Foreground),
                TextWrapping = tb.TextWrapping, TextAlignment = tb.TextAlignment,
                VerticalAlignment = tb.VerticalAlignment,
                HorizontalAlignment = tb.HorizontalAlignment,
                Margin = tb.Margin, FontWeight = tb.FontWeight,
            },
        };

        var rotate = new RotateTransform();
        var translate = new TranslateTransform();
        var tg = new TransformGroup();
        tg.Children.Add(rotate);
        tg.Children.Add(translate);
        _ghost.RenderTransform = tg;

        Canvas.SetLeft(_ghost, _ghostCanvasPos.X);
        Canvas.SetTop(_ghost, _ghostCanvasPos.Y);
        DragOverlay.Children.Add(_ghost);

        var wobble = new DoubleAnimationUsingKeyFrames();
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(6, TimeSpan.FromSeconds(0.08)));
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(-4, TimeSpan.FromSeconds(0.16)));
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(3, TimeSpan.FromSeconds(0.24)));
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(-1.5, TimeSpan.FromSeconds(0.32)));
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(0.8, TimeSpan.FromSeconds(0.40)));
        wobble.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromSeconds(0.48)));
        rotate.BeginAnimation(RotateTransform.AngleProperty, wobble);

        Mouse.Capture(this);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_ghost == null || _draggedCard == null) return;
        if (_ghost.RenderTransform is not TransformGroup tg) return;

        var t = (TranslateTransform)tg.Children[1];
        var pos = e.GetPosition(DragOverlay);
        t.X = pos.X - _dragStartMouse.X;
        t.Y = pos.Y - _dragStartMouse.Y;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_ghost == null || _draggedCard == null || _dragBorder == null)
        {
            CleanupDrag();
            return;
        }

        var mousePos = e.GetPosition(this);
        var targetCat = FindCategoryAtPoint(mousePos);

        if (targetCat != null)
            AnimateDrop(targetCat);
        else
            AnimateReturn();
    }

    private void AnimateDrop(SortCategoryViewModel cat)
    {
        var ghost = _ghost;
        var border = _dragBorder;
        var card = _draggedCard;
        if (ghost == null || border == null || card == null) return;
        if (!_catPanels.TryGetValue(cat, out var catPanel)) { AnimateReturn(); return; }

        Point catOrigin;
        try { catOrigin = catPanel.TranslatePoint(new Point(0, 0), DragOverlay); }
        catch { AnimateReturn(); return; }

        if (ghost.RenderTransform is not TransformGroup tg) { CleanupDrag(); return; }
        var t = (TranslateTransform)tg.Children[1];

        var targetTX = catOrigin.X - _ghostCanvasPos.X;
        var targetTY = catOrigin.Y - _ghostCanvasPos.Y;

        var animX = new DoubleAnimation(t.X, targetTX, TimeSpan.FromMilliseconds(80))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
        var animY = new DoubleAnimation(t.Y, targetTY, TimeSpan.FromMilliseconds(80))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };

        var done = false;
        animX.Completed += (_, _) =>
        {
            if (done) return;
            done = true;

            DragOverlay.Children.Remove(ghost);

            if (!_cardWasInCategory)
            {
                border.Visibility = Visibility.Visible;
                if (CardsPanel.Children.Contains(border))
                    CardsPanel.Children.Remove(border);
            }
            catPanel.Children.Add(border);
            _placed[card] = true;
            _cardCategory[card] = cat;
            CheckAllPlaced();
            CleanupDrag();
        };

        Storyboard.SetTarget(animX, t);
        Storyboard.SetTargetProperty(animX, new PropertyPath(TranslateTransform.XProperty));
        Storyboard.SetTarget(animY, t);
        Storyboard.SetTargetProperty(animY, new PropertyPath(TranslateTransform.YProperty));
        var sb = new Storyboard();
        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }

    private void AnimateReturn()
    {
        var ghost = _ghost;
        var border = _dragBorder;
        var card = _draggedCard;
        if (ghost == null || border == null || card == null)
        {
            CleanupDrag();
            return;
        }

        if (_cardWasInCategory)
        {
            DragOverlay.Children.Remove(ghost);
            border.Visibility = Visibility.Visible;
            CardsPanel.Children.Add(border);
            _placed[card] = false;
            _cardCategory[card] = null;
            CheckAllPlaced();
            CleanupDrag();
            return;
        }

        if (ghost.RenderTransform is not TransformGroup tg) { CleanupDrag(); return; }
        var t = (TranslateTransform)tg.Children[1];

        var animX = new DoubleAnimation(t.X, 0, TimeSpan.FromMilliseconds(150))
        { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };
        var animY = new DoubleAnimation(t.Y, 0, TimeSpan.FromMilliseconds(150))
        { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };

        var done = false;
        animX.Completed += (_, _) =>
        {
            if (done) return;
            done = true;

            DragOverlay.Children.Remove(ghost);
            border.Visibility = Visibility.Visible;
            CheckAllPlaced();
            CleanupDrag();
        };

        Storyboard.SetTarget(animX, t);
        Storyboard.SetTargetProperty(animX, new PropertyPath(TranslateTransform.XProperty));
        Storyboard.SetTarget(animY, t);
        Storyboard.SetTargetProperty(animY, new PropertyPath(TranslateTransform.YProperty));
        var sb = new Storyboard();
        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }

    private SortCategoryViewModel? FindCategoryAtPoint(Point pagePoint)
    {
        foreach (var (cat, border) in _catBorders)
        {
            try
            {
                var topLeft = border.TranslatePoint(new Point(0, 0), this);
                if (double.IsNaN(topLeft.X) || double.IsNaN(topLeft.Y)) continue;
                var rect = new Rect(topLeft.X, topLeft.Y, border.ActualWidth, border.ActualHeight);
                if (rect.Contains(pagePoint)) return cat;
            }
            catch { }
        }
        return null;
    }

    private void CheckAllPlaced()
    {
        NextBtn.IsEnabled = _placed.Values.All(p => p);
    }

    private void CleanupDrag()
    {
        _dragBorder = null;
        _draggedCard = null;
        _ghost = null;
        _isDragging = false;
        _dragSourceCategory = null;
        Mouse.Capture(null);
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        NextBtn.Visibility = Visibility.Collapsed;
        ContinueBtn.Visibility = Visibility.Visible;

        _isDragging = true;
        CleanupDrag();
        HelpText.Text = "Проверьте раскладку карточек";

        foreach (var (vm, border) in _cardBorders)
        {
            if (!_placed[vm]) continue;
            var cat = _cardCategory[vm];
            bool correct = cat != null && vm.CategoryId == cat.Index;
            border.BorderThickness = new Thickness(2.5);
            border.Background = correct ? HexBrush("#F0FFF4") : HexBrush("#FFF5F5");
            border.BorderBrush = correct ? HexBrush("#48BB78") : HexBrush("#FC8181");
            border.Cursor = Cursors.Arrow;
            border.PreviewMouseLeftButtonDown -= Card_PreviewMouseDown;
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_nav is MainWindow mw && mw._currentSession is { } session)
        {
            session.SortCorrect = _cards.Count(c => _placed[c] && _cardCategory[c] != null && c.CategoryId == _cardCategory[c]!.Index);
            session.SortTotal = _cards.Count;
            session.SortCategories = new List<SortCategoryViewModel>(_categories);
            session.SortCards = new List<SortCardViewModel>(_cards);
            mw.ShowCase(session.Topic, session);
        }
    }
}
