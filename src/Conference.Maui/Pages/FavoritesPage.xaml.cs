using Conference.Maui.Models;
using Conference.Maui.Services;
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
#if ANDROID
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
#endif
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }

#if ANDROID
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FavoritesViewModel.FavoriteSlots))
        {
            ResetStickyHeader();
        }
    }
#endif

    private async void OnConflictBadgeTapped(object? sender, EventArgs e)
    {
        if (!_viewModel.IsConflictResolverEnabled) return;
        await Shell.Current.GoToAsync(nameof(ConflictResolverPage));
    }

#if ANDROID
    private int _lastStickyGroupIndex = -1;

    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        var groups = _viewModel.FavoriteSlots;
        if (groups == null || groups.Count == 0)
        {
            StickyHeaderOverlay.IsVisible = false;
            return;
        }

        int groupIndex = GetGroupIndexForStickyHeader(e.FirstVisibleItemIndex, groups);
        bool shouldShow = groupIndex >= 0 && e.VerticalOffset > 30;
        StickyHeaderOverlay.IsVisible = shouldShow;

        if (shouldShow && groupIndex >= 0)
        {
            var group = groups[groupIndex];
            StickyHeaderLabel.Text = group.TimeDisplay;
            StickyHeaderConflict.IsVisible = group.HasConflict;
            if (group.HasConflict)
                StickyHeaderConflict.Text = $"⚠ {group.ConflictText}";

            if (groupIndex != _lastStickyGroupIndex)
            {
                _lastStickyGroupIndex = groupIndex;
                if (HapticService.IsEnabled)
                {
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
                    catch { /* Haptics not available */ }
                }
            }
        }
        else
        {
            _lastStickyGroupIndex = -1;
        }
    }

    private void ResetStickyHeader()
    {
        _lastStickyGroupIndex = -1;
        StickyHeaderOverlay.IsVisible = false;
    }

    private static int GetGroupIndexForStickyHeader(int flatIndex, IReadOnlyList<TimeSlotGroup> groups)
    {
        if (flatIndex < 0) return -1;

        int cumulative = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            cumulative += groups[i].Count;
            if (flatIndex < cumulative)
            {
                int groupStart = cumulative - groups[i].Count;
                if (flatIndex == groupStart && i > 0)
                    return i - 1;
                return i;
            }
        }
        return groups.Count - 1;
    }
#else
    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e) { }
#endif
}
