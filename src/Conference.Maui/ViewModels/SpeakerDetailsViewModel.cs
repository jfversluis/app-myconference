using CommunityToolkit.Mvvm.ComponentModel;
using Conference.Maui.Models;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SelectedSpeaker), nameof(SelectedSpeaker))]
public partial class SpeakerDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    Speaker? _selectedSpeaker;
}
