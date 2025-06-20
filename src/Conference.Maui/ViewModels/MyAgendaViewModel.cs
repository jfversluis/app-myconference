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

    public async Task LoadFavoriteSessionsAsync()
    {
        // Get all sessions
        var allSessions = await _eventService.GetAllSessions();
        
        // Get favorite session IDs
        var favoriteSessionData = await _databaseService.GetAllFavoriteSessionsAsync();
        var favoriteSessionIds = favoriteSessionData
            .Where(f => f.IsFavorite)
            .Select(f => f.SessionId)
            .ToHashSet();

        // Filter sessions to only include favorites
        var favoriteSessions = allSessions
            .Where(s => favoriteSessionIds.Contains(s.Id))
            .OrderBy(s => s.StartsAt)
            .ToList();

        // Update the observable collection
        FavoriteSessions.Clear();
        foreach (var session in favoriteSessions)
        {
            FavoriteSessions.Add(session);
        }
        
        // Also update the general Sessions collection for compatibility
        Sessions.Clear();
        foreach (var session in favoriteSessions)
        {
            Sessions.Add(session);
        }
    }

    [RelayCommand]
    private async Task RefreshFavorites()
    {
        await LoadFavoriteSessionsAsync();
    }

    [RelayCommand]
    private async Task RemoveFromFavorites(Session session)
    {
        await _databaseService.DeleteFavoriteSessionAsync(session.Id);
        await LoadFavoriteSessionsAsync(); // Refresh the list
    }
}
