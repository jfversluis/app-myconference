using Conference.Maui.ViewModels;
using Plugin.Maui.SwipeCardView.Core;

namespace Conference.Maui.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        UpdateStepVisibility();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    protected override bool OnBackButtonPressed() => true;

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingViewModel.CurrentStep))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateStepVisibility();
                AnimateStepTransition();
            });
        }
    }

    private void UpdateStepVisibility()
    {
        WelcomeStep.IsVisible = _viewModel.CurrentStep == 0;
        InterestsStep.IsVisible = _viewModel.CurrentStep == 1;
        QuickPickStep.IsVisible = _viewModel.CurrentStep == 2;
        DoneStep.IsVisible = _viewModel.CurrentStep == 3;

        if (_viewModel.CurrentStep == 3)
            UpdateSummaryText();
    }

    private async void AnimateStepTransition()
    {
        var currentView = _viewModel.CurrentStep switch
        {
            0 => (View)WelcomeStep,
            1 => InterestsStep,
            2 => QuickPickStep,
            3 => DoneStep,
            _ => WelcomeStep
        };

        currentView.Opacity = 0;
        currentView.TranslationX = 30;
        await Task.WhenAll(
            currentView.FadeToAsync(1, 300, Easing.CubicOut),
            currentView.TranslateToAsync(0, 0, 300, Easing.CubicOut));
    }

    private void UpdateSummaryText()
    {
        var count = _viewModel.AddedCount;
        SummaryLabel.Text = count switch
        {
            0 => "No sessions added yet — no worries! You can build your agenda anytime.",
            1 => "You added 1 session to your agenda. Great start!",
            _ => $"You added {count} sessions to your agenda. Great picks!"
        };
    }

    private async void OnSwiped(object sender, SwipedCardEventArgs e)
    {
        if (e.Item is Models.SessionItem session)
            await _viewModel.HandleSwipeAsync(session, e.Direction);
    }

    private void OnDragging(object sender, DraggingCardEventArgs e)
    {
    }

    private async void OnViewAgendaClicked(object? sender, EventArgs e)
    {
        OnboardingViewModel.MarkOnboardingCompleted();
        if (Navigation.ModalStack.Count > 0)
            await Navigation.PopModalAsync(animated: true);
        await Task.Delay(200);
        await Shell.Current.GoToAsync("//Favorites");
    }
}
