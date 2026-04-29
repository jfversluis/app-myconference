using Conference.Maui.Models;
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

}
