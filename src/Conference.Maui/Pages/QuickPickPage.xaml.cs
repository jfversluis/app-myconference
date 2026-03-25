using Conference.Maui.Models;
using Conference.Maui.ViewModels;
using Plugin.Maui.SwipeCardView.Core;

namespace Conference.Maui.Pages;

public partial class QuickPickPage : ContentPage
{
    private readonly QuickPickViewModel _viewModel;
    private bool _isDragging;

    public QuickPickPage(QuickPickViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        SwipeCardView.Swiped += OnSwiped;
        SwipeCardView.Dragging += OnDragging;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDeckAsync();
    }

    private async void OnSwiped(object? sender, SwipedCardEventArgs e)
    {
        await _viewModel.HandleSwipeAsync(e.Item as SessionItem, e.Direction);
    }

    private void OnDragging(object? sender, DraggingCardEventArgs e)
    {
        if (e.CardView == null) return;

        var likeFrame = e.CardView.FindByName<Border>("LikeFrame");
        var nopeFrame = e.CardView.FindByName<Border>("NopeFrame");

        if (likeFrame == null || nopeFrame == null) return;

        const double threshold = 100.0;
        var dragPercent = e.DistanceDraggedX / threshold;

        switch (e.Position)
        {
            case DraggingCardPosition.Start:
                _isDragging = true;
                likeFrame.Opacity = 0;
                nopeFrame.Opacity = 0;
                break;

            case DraggingCardPosition.UnderThreshold:
            case DraggingCardPosition.OverThreshold:
                if (dragPercent > 0)
                {
                    likeFrame.Opacity = Math.Min(dragPercent, 1.0);
                    nopeFrame.Opacity = 0;
                    // Animate Add button up, Skip button down
                    AnimateButtonDuringDrag(AddButton, Math.Min(dragPercent, 1.0));
                    AnimateButtonDuringDrag(SkipButton, 0);
                }
                else if (dragPercent < 0)
                {
                    nopeFrame.Opacity = Math.Min(Math.Abs(dragPercent), 1.0);
                    likeFrame.Opacity = 0;
                    // Animate Skip button up, Add button down
                    AnimateButtonDuringDrag(SkipButton, Math.Min(Math.Abs(dragPercent), 1.0));
                    AnimateButtonDuringDrag(AddButton, 0);
                }
                break;

            case DraggingCardPosition.FinishedOverThreshold:
            case DraggingCardPosition.FinishedUnderThreshold:
                _isDragging = false;
                likeFrame.Opacity = 0;
                nopeFrame.Opacity = 0;
                ResetButtonAnimation(AddButton);
                ResetButtonAnimation(SkipButton);
                break;
        }
    }

    private static void AnimateButtonDuringDrag(Border button, double intensity)
    {
        // Scale up slightly and increase opacity as user drags toward this action
        var scale = 1.0 + (intensity * 0.15); // up to 1.15x
        button.Scale = scale;
    }

    private static void ResetButtonAnimation(Border button)
    {
        button.ScaleToAsync(1.0, 150, Easing.CubicOut);
    }

    /// <summary>
    /// "Details ›" label tapped — navigate to session details as a modal.
    /// </summary>
    private async void OnDetailsTapped(object? sender, TappedEventArgs e)
    {
        if (_isDragging) return;

        if (sender is View view && view.BindingContext is SessionItem session)
        {
            await Shell.Current.GoToAsync("SessionDetailsPage", new Dictionary<string, object>
            {
                ["SessionId"] = session.Id
            });
        }
    }

    /// <summary>
    /// Skip button tapped — programmatic left swipe.
    /// </summary>
    private void OnSkipTapped(object? sender, TappedEventArgs e)
    {
        SwipeCardView.InvokeSwipe(SwipeCardDirection.Left);
    }

    /// <summary>
    /// Add button tapped — programmatic right swipe.
    /// </summary>
    private void OnAddTapped(object? sender, TappedEventArgs e)
    {
        SwipeCardView.InvokeSwipe(SwipeCardDirection.Right);
    }

    private async void OnUndoTapped(object? sender, EventArgs e)
    {
        var success = SwipeCardView.GoBack(animated: true);
        if (success)
        {
            await _viewModel.UndoLastSwipeCommand.ExecuteAsync(null);
        }
    }
}
