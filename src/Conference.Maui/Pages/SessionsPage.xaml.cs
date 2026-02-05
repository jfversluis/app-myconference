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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_viewModel.Days.Count == 0)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
