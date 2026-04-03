using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class FavoritesPage : ContentPage, IScrollToTop
{
    public void ScrollToTop()
    {
        if (_viewModel.FavoriteSlots.Count > 0)
            FavoritesCollectionView.ScrollTo(0);
    }

    private readonly FavoritesViewModel _viewModel;

    public FavoritesPage(FavoritesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        var session = e.CurrentSelection.FirstOrDefault() as SessionItem;
        if (session != null)
        {
            FavoritesCollectionView.SelectedItem = null;

            await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
            {
                ["SessionId"] = session.Id
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }

    private async void OnConflictBadgeTapped(object? sender, EventArgs e)
    {
        if (!_viewModel.IsConflictResolverEnabled) return;
        await Shell.Current.GoToAsync(nameof(ConflictResolverPage));
    }
}
