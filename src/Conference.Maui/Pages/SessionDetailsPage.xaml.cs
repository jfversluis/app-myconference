using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SessionDetailsPage : ContentPage
{
    private readonly SessionDetailsViewModel _viewModel;

    public SessionDetailsPage(SessionDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnSpeakerTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is SpeakerItem speaker)
        {
            await _viewModel.NavigateToSpeakerCommand.ExecuteAsync(speaker);
        }
    }
}
