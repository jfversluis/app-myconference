using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class AboutPage : ContentPage
{
    private readonly AboutViewModel _viewModel;

    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}
