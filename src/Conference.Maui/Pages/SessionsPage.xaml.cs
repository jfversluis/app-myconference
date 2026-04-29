using Conference.Maui.Models;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Syncfusion.Maui.Toolkit.TabView;

namespace Conference.Maui.Pages;

public partial class SessionsPage : ContentPage, IScrollToTop
{
    public void ScrollToTop()
    {
        if (_viewModel.CurrentDaySlots.Count > 0)
            SessionsCollectionView.ScrollTo(0);
    }

    private readonly SessionsViewModel _viewModel;

    public SessionsPage(SessionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionsViewModel.Days))
        {
            PopulateDayTabs();
        }
#if ANDROID
        if (e.PropertyName == nameof(SessionsViewModel.CurrentDaySlots))
        {
            ResetStickyHeader();
        }
#endif
    }

    private void PopulateDayTabs()
    {
        DaySwitcher.Items.Clear();
        foreach (var day in _viewModel.Days)
        {
            var tabItem = new SfTabItem { Header = day.DisplayName };
            tabItem.SetAppTheme(SfTabItem.TextColorProperty,
                Color.FromArgb("#333333"), Color.FromArgb("#D0D0D0"));
            DaySwitcher.Items.Add(tabItem);
        }
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        var session = e.CurrentSelection.FirstOrDefault() as SessionItem;
        if (session != null)
        {
            // Clear selection immediately to remove visual highlight
            SessionsCollectionView.SelectedItem = null;
            
            await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
            {
                ["SessionId"] = session.Id
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_viewModel.Days.Count == 0)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
        else if (DaySwitcher.Items.Count == 0)
        {
            // Data loaded while we were off-screen — heal the missed PropertyChanged
            PopulateDayTabs();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

#if ANDROID
    private int _lastStickyGroupIndex = -1;

    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        var groups = _viewModel.CurrentDaySlots;
        if (groups == null || groups.Count == 0)
        {
            StickyHeaderOverlay.IsVisible = false;
            return;
        }

        int groupIndex = GetGroupIndexFromFlatIndex(e.FirstVisibleItemIndex, groups);
        bool shouldShow = groupIndex >= 0 && e.VerticalOffset > 30;
        StickyHeaderOverlay.IsVisible = shouldShow;

        if (shouldShow && groupIndex >= 0)
        {
            StickyHeaderLabel.Text = groups[groupIndex].TimeDisplay;

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

    private static int GetGroupIndexFromFlatIndex(int flatIndex, IReadOnlyList<TimeSlotGroup> groups)
    {
        if (flatIndex < 0) return -1;

        // Add +1 offset: FirstVisibleItemIndex may point to a barely-visible
        // trailing item from the previous group at the viewport top edge.
        int adjustedIndex = flatIndex + 1;
        int cumulative = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            int groupSize = 1 + groups[i].Count; // header + items in flat adapter
            if (adjustedIndex < cumulative + groupSize)
                return i;
            cumulative += groupSize;
        }
        return groups.Count - 1;
    }
#else
    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e) { }
#endif
}
