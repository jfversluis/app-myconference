using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SponsorsPage : ContentPage
{
    private readonly SponsorsViewModel _viewModel;

    public SponsorsPage(SponsorsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSponsorsData();
    }
}