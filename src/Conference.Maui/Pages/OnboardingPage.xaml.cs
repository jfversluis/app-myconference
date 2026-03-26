using Conference.Maui.ViewModels;
using Plugin.Maui.SwipeCardView.Core;

namespace Conference.Maui.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    // Circle borders and images for speaker photos (populated in code-behind)
    private Border[] _speakerCircles = [];
    private Image[] _speakerImages = [];

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _speakerCircles = [SpeakerCircle1, SpeakerCircle2, SpeakerCircle3, SpeakerCircle4, SpeakerCircle5];
        _speakerImages = [SpeakerImg1, SpeakerImg2, SpeakerImg3, SpeakerImg4, SpeakerImg5];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        PopulateSpeakerPhotos();
        UpdateStepVisibility();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    protected override bool OnBackButtonPressed() => true;

    private void PopulateSpeakerPhotos()
    {
        var speakers = _viewModel.FeaturedSpeakers;

        // Show fallback if no speakers
        if (speakers.Count == 0)
        {
            WelcomeFallback.IsVisible = true;
            SpeakerPhotosLayout.IsVisible = false;
            return;
        }

        WelcomeFallback.IsVisible = false;
        SpeakerPhotosLayout.IsVisible = true;

        for (int i = 0; i < Math.Min(speakers.Count, _speakerCircles.Length); i++)
        {
            _speakerImages[i].Source = ImageSource.FromUri(new Uri(speakers[i].ProfilePictureUrl));
            _speakerCircles[i].Stroke = Color.FromArgb(speakers[i].CircleColor);
            _speakerCircles[i].BackgroundColor = Color.FromArgb(speakers[i].CircleColor);
            _speakerCircles[i].IsVisible = true;
        }

        // Hide unused circles
        for (int i = speakers.Count; i < _speakerCircles.Length; i++)
        {
            _speakerCircles[i].IsVisible = false;
        }

        // Stagger entrance animations
        AnimateSpeakerPhotos();
    }

    private async void AnimateSpeakerPhotos()
    {
        foreach (var circle in _speakerCircles)
        {
            if (!circle.IsVisible) continue;
            circle.Opacity = 0;
            circle.Scale = 0.6;
        }

        for (int i = 0; i < _speakerCircles.Length; i++)
        {
            if (!_speakerCircles[i].IsVisible) continue;
            var circle = _speakerCircles[i];
            _ = circle.FadeToAsync(1, 400, Easing.CubicOut);
            _ = circle.ScaleToAsync(1, 500, Easing.SpringOut);
            await Task.Delay(100);
        }
    }

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
        NotificationStep.IsVisible = _viewModel.CurrentStep == 1;
        InterestsStep.IsVisible = _viewModel.CurrentStep == 2;
        QuickPickStep.IsVisible = _viewModel.CurrentStep == 3;
        DoneStep.IsVisible = _viewModel.CurrentStep == 4;

        if (_viewModel.CurrentStep == 4)
            UpdateSummaryText();
    }

    private async void AnimateStepTransition()
    {
        var currentView = _viewModel.CurrentStep switch
        {
            0 => (View)WelcomeStep,
            1 => NotificationStep,
            2 => InterestsStep,
            3 => QuickPickStep,
            4 => DoneStep,
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
