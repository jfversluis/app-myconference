using Conference.Maui.ViewModels;
using Plugin.Maui.SwipeCardView.Core;
using Plugin.Maui.SwipeCardView;

namespace Conference.Maui.Pages;

public partial class PickFavoriteSessionsPage : ContentPage
{
    readonly PickFavoriteSessionsViewModel _viewModel;
    public PickFavoriteSessionsPage(PickFavoriteSessionsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        _viewModel = vm;
    }
    private void OnDislikeClicked(object sender, EventArgs e)
    {
        SwipeCardView.InvokeSwipe(SwipeCardDirection.Left);
    }

    private void OnSuperLikeClicked(object sender, EventArgs e)
    {
        SwipeCardView.InvokeSwipe(SwipeCardDirection.Up);
    }

    private void OnLikeClicked(object sender, EventArgs e)
    {
        SwipeCardView.InvokeSwipe(SwipeCardDirection.Right);
    }

    private void OnDragging(object sender, DraggingCardEventArgs e)
    {
        var view = (View)sender;
        var NopeFrame = view.FindByName<Border>("NopeFrame");
        var LikeFrame = view.FindByName<Border>("LikeFrame");
        //var SuperLikeFrame = view.FindByName<Frame>("SuperLikeFrame");
        var threshold = _viewModel.Threshold;

        var draggedXPercent = e.DistanceDraggedX / threshold;

        var draggedYPercent = e.DistanceDraggedY / threshold;

        switch (e.Position)
        {
            case DraggingCardPosition.Start:
                NopeFrame.Opacity = 0;
                LikeFrame.Opacity = 0;
                //SuperLikeFrame.Opacity = 0;
                nopeButton.Scale = 1;
                likeButton.Scale = 1;
                //superLikeButton.Scale = 1;
                break;

            case DraggingCardPosition.UnderThreshold:
                if (e.Direction == SwipeCardDirection.Left)
                {
                    NopeFrame.Opacity = (-1) * draggedXPercent;
                    nopeButton.Scale = 1 + draggedXPercent / 2;
                    //SuperLikeFrame.Opacity = 0;
                    //superLikeButton.Scale = 1;
                }
                else if (e.Direction == SwipeCardDirection.Right)
                {
                    LikeFrame.Opacity = draggedXPercent;
                    likeButton.Scale = 1 - draggedXPercent / 2;
                    //SuperLikeFrame.Opacity = 0;
                    //superLikeButton.Scale = 1;
                }
                else if (e.Direction == SwipeCardDirection.Up)
                {
                    NopeFrame.Opacity = 0;
                    LikeFrame.Opacity = 0;
                    nopeButton.Scale = 1;
                    likeButton.Scale = 1;
                    //SuperLikeFrame.Opacity = (-1) * draggedYPercent;
                    //superLikeButton.Scale = 1 + draggedYPercent / 2;
                }
                break;

            case DraggingCardPosition.OverThreshold:
                if (e.Direction == SwipeCardDirection.Left)
                {
                    NopeFrame.Opacity = 1;
                    //SuperLikeFrame.Opacity = 0;
                }
                else if (e.Direction == SwipeCardDirection.Right)
                {
                    LikeFrame.Opacity = 1;
                    //SuperLikeFrame.Opacity = 0;
                }
                else if (e.Direction == SwipeCardDirection.Up)
                {
                    NopeFrame.Opacity = 0;
                    LikeFrame.Opacity = 0;
                    //SuperLikeFrame.Opacity = 1;
                }
                break;

            case DraggingCardPosition.FinishedUnderThreshold:
                NopeFrame.Opacity = 0;
                LikeFrame.Opacity = 0;
                //SuperLikeFrame.Opacity = 0;
                nopeButton.Scale = 1;
                likeButton.Scale = 1;
                //superLikeButton.Scale = 1;
                break;

            case DraggingCardPosition.FinishedOverThreshold:
                NopeFrame.Opacity = 0;
                LikeFrame.Opacity = 0;
                //SuperLikeFrame.Opacity = 0;
                nopeButton.Scale = 1;
                likeButton.Scale = 1;
                //superLikeButton.Scale = 1;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}