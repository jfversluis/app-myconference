using CommunityToolkit.Mvvm.ComponentModel;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
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
}
