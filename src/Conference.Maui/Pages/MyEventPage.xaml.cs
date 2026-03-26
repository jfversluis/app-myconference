using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class MyEventPage : ContentPage
{
    private readonly MyEventViewModel _viewModel;

    public MyEventPage(MyEventViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
#if DEBUG
        AddDebugTimePanel();
#endif
    }

#if DEBUG
    private void AddDebugTimePanel()
    {
        var debugLabel = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD54F"),
            VerticalOptions = LayoutOptions.Center
        };
        debugLabel.SetBinding(Label.TextProperty, nameof(MyEventViewModel.DebugTimeDisplay));

        var travelButton = new Button
        {
            Text = "⏱ Travel",
            BackgroundColor = Color.FromArgb("#7C4DFF"),
            TextColor = Colors.White,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 6,
            Padding = new Thickness(12, 6),
            HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center
        };
        travelButton.SetBinding(Button.CommandProperty, nameof(MyEventViewModel.CycleDebugTimeCommand));

        var stack = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Children = { debugLabel, travelButton }
        };

        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Background = new SolidColorBrush(Color.FromArgb("#E0333333")),
            Padding = new Thickness(12, 8),
            Content = stack
        };

        var container = new Grid
        {
            VerticalOptions = LayoutOptions.End,
            Padding = new Thickness(12, 0, 12, 100),
            ZIndex = 100,
            InputTransparent = false,
            Children = { border }
        };
        container.SetBinding(IsVisibleProperty, nameof(MyEventViewModel.ShowDebugPanel));

        if (Content is Grid rootGrid)
        {
            rootGrid.Children.Add(container);
        }
    }
#endif

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        _viewModel.StartTimer();
        StartLiveBadgePulse();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopTimer();
        LiveBadge.CancelAnimations();
    }

    private void StartLiveBadgePulse()
    {
        LiveBadge.CancelAnimations();
        // Gentle back-and-forth pulse: 1.0 → 0.6 → 1.0
        var fadeOut = new Animation(v => LiveBadge.Opacity = v, 1.0, 0.6, Easing.SinInOut);
        var fadeIn = new Animation(v => LiveBadge.Opacity = v, 0.6, 1.0, Easing.SinInOut);
        var pulse = new Animation();
        pulse.Add(0, 0.5, fadeOut);
        pulse.Add(0.5, 1.0, fadeIn);
        pulse.Commit(LiveBadge, "LivePulse", length: 2000, repeat: () => true);
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SessionItem session)
        {
            await _viewModel.NavigateToSessionCommand.ExecuteAsync(session);

            if (sender is CollectionView cv)
                cv.SelectedItem = null;
        }
    }

    private async void OnSessionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is SessionItem session)
        {
            await _viewModel.NavigateToSessionCommand.ExecuteAsync(session);
        }
    }
}
