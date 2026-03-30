using Conference.Maui.Models;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
#if IOS
using UIKit;
#endif

namespace Conference.Maui.Pages;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _viewModel;
    private int _lastStickySection = -1;

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
        _lastStickySection = -1;
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }

    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        int section = -1;

#if IOS
        if (FavoritesCollectionView.Handler?.PlatformView is UICollectionView cv)
        {
            var visiblePaths = cv.IndexPathsForVisibleItems;
            if (visiblePaths is { Length: > 0 })
            {
                nint topSection = nint.MaxValue;
                foreach (var path in visiblePaths)
                {
                    if (path.Section < topSection)
                        topSection = path.Section;
                }
                if (topSection < nint.MaxValue)
                    section = (int)topSection;
            }
        }
#endif

        if (section < 0)
        {
            var slots = _viewModel.FavoriteSlots;
            if (slots == null || slots.Count == 0) return;

            int remaining = e.FirstVisibleItemIndex;
            section = 0;
            foreach (var group in slots)
            {
                if (remaining < group.Count) break;
                remaining -= group.Count;
                section++;
            }
            if (section >= slots.Count) section = slots.Count - 1;
        }

        if (section < 0) return;

        if (_lastStickySection >= 0 && section != _lastStickySection)
        {
            if (HapticService.IsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        _lastStickySection = section;
    }

    private async void OnConflictBadgeTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ConflictResolverPage));
    }
}
