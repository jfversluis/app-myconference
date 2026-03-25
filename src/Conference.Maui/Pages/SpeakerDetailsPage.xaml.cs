using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SpeakerDetailsPage : ContentPage
{
    private readonly SpeakerDetailsViewModel _viewModel;

    public SpeakerDetailsPage(SpeakerDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnSessionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is SessionItem session)
        {
            await _viewModel.NavigateToSessionCommand.ExecuteAsync(session);
        }
    }
}
