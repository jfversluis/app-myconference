using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Plugin.Maui.SwipeCardView.Core;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class MyAgendaViewModel : ObservableObject
{
    private readonly IEventDataService _eventService;
    private readonly IDatabaseService _databaseService;
   
    

    public ObservableCollection<Session> FavoriteSessions { get; set; } = [];
    public ObservableCollection<Session> Sessions { get; set; } = [];
    

    
    public MyAgendaViewModel(IEventDataService eventService, IDatabaseService databaseService)
    {
        _eventService = eventService;
        _databaseService = databaseService;
    }
   

    

}
