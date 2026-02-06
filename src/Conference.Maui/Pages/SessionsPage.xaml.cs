using Conference.Maui.Models;
using Conference.Maui.ViewModels;
using Syncfusion.Maui.Toolkit.TabView;

namespace Conference.Maui.Pages;

public partial class SessionsPage : ContentPage
{
    private readonly SessionsViewModel _viewModel;

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
            DaySwitcher.Items.Add(new SfTabItem { Header = day.DisplayName });
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
        
        if (_viewModel.Days.Count == 0)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
