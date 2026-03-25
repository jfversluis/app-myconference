using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SpeakersPage : ContentPage
{
    private readonly SpeakersViewModel _viewModel;

    public SpeakersPage(SpeakersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnSpeakerSelected(object? sender, SelectionChangedEventArgs e)
    {
        var speaker = e.CurrentSelection.FirstOrDefault() as SpeakerItem;
        if (speaker != null)
        {
            SpeakersCollectionView.SelectedItem = null;

            await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage), new Dictionary<string, object>
            {
                ["SpeakerId"] = speaker.Id
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.Speakers.Count == 0)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}
