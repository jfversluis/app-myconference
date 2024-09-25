using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SelectedSession), nameof(SelectedSession))]
public partial class SessionDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    Session? _selectedSession;

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        await Shell.Current.GoToAsync("SpeakerDetails",
            new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
    }
}
