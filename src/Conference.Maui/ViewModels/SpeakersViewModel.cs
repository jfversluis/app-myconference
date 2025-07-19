using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class SpeakersViewModel(IEventDataService eventDataService) : ObservableObject
{
    private readonly IEventDataService _eventService = eventDataService;

    public ObservableCollection<Speaker> Speakers { get; set; } = [];

    public async Task LoadSpeakersData()
    {
        var speakers = await _eventService.GetAllSpeakers();

        Speakers.Clear();
        foreach (var speaker in speakers)
        {
            Speakers.Add(speaker);
        }
    }

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage),
            new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
    }
}
