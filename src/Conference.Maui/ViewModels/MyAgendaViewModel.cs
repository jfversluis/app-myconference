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
    private bool _isInitialized = false;

    public ObservableCollection<Session> FavoriteSessions { get; set; } = [];
    public ObservableCollection<Session> Sessions { get; set; } = [];

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool hasFavorites = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public MyAgendaViewModel(IEventDataService eventService, IDatabaseService databaseService)
    {
        _eventService = eventService;
        _databaseService = databaseService;
    }

    public async Task InitializeAsync()
    {
        // If already initialized and we have some data loaded, just return instantly
        if (_isInitialized)
        {
            return;
        }

        await LoadFavoriteSessionsAsync();
        _isInitialized = true;
    }

    public async Task LoadFavoriteSessionsAsync()
    {
        // Skip loading indicator if we already have data (for subsequent navigations)
        bool shouldShowLoading = !HasFavorites && FavoriteSessions.Count == 0;
        
        if (IsLoading)
            return;

        try
        {
            if (shouldShowLoading)
            {
                IsLoading = true;
            }
            ErrorMessage = string.Empty;

            // Get all sessions
            var allSessions = await _eventService.GetAllSessions();

            // Get favorite session IDs
            var favoriteSessionsDb = await _databaseService.GetAllFavoriteSessionsAsync();
            var favoriteIds = favoriteSessionsDb.Where(f => f.IsFavorite).Select(f => f.SessionId).ToHashSet();

            // Filter sessions to only favorites
            var favoriteSessions = allSessions.Where(s => favoriteIds.Contains(s.Id)).ToList();

            // Update the favorite status
            foreach (var session in favoriteSessions)
            {
                session.IsFavorite = true;
            }

            FavoriteSessions.Clear();
            foreach (var session in favoriteSessions.OrderBy(s => s.StartsAt))
            {
                FavoriteSessions.Add(session);
            }

            HasFavorites = FavoriteSessions.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load favorite sessions: {ex.Message}";
            HasFavorites = false;
        }
        finally
        {
            if (shouldShowLoading)
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task RemoveFromFavorites(Session session)
    {
        try
        {
            await _databaseService.DeleteFavoriteSessionAsync(session.Id);
            session.IsFavorite = false;
            FavoriteSessions.Remove(session);
            HasFavorites = FavoriteSessions.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to remove from favorites: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadFavoriteSessionsAsync();
    }
}
