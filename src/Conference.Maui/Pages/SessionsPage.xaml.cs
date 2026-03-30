using Conference.Maui.Models;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Syncfusion.Maui.Toolkit.TabView;
#if IOS
using UIKit;
#endif

namespace Conference.Maui.Pages;

public partial class SessionsPage : ContentPage
{
    private readonly SessionsViewModel _viewModel;
    private int _lastStickySection = -1;

    public SessionsPage(SessionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionsViewModel.Days))
        {
            PopulateDayTabs();
        }
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
        _lastStickySection = -1;
        
        if (_viewModel.Days.Count == 0)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }

    private void OnCollectionViewScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        int section = -1;

#if IOS
        // Query native UICollectionView for the actual topmost visible section
        if (SessionsCollectionView.Handler?.PlatformView is UICollectionView cv)
        {
            var visiblePaths = cv.IndexPathsForVisibleItems;
            if (visiblePaths == null || visiblePaths.Length == 0) return;

            nint topSection = nint.MaxValue;
            foreach (var path in visiblePaths)
            {
                if (path.Section < topSection)
                    topSection = path.Section;
            }

            if (topSection < nint.MaxValue)
                section = (int)topSection;
        }
#else
        // Fallback: map flat FirstVisibleItemIndex to section
        var slots = _viewModel.CurrentDaySlots;
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
#endif

        if (section < 0) return;

        if (_lastStickySection >= 0 && section != _lastStickySection)
        {
            if (HapticService.IsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        _lastStickySection = section;
    }
}
