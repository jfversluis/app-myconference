using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Services;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly ISessionizeService _sessionizeService;
    private readonly IFavoritesService _favoritesService;
    private List<Session> _allSessions = new();

    [ObservableProperty]
    private ObservableCollection<TimeSlot> groupedFavorites = new();

    [ObservableProperty]
    private bool hasFavorites;

    [ObservableProperty]
    private bool isRefreshing;

    public FavoritesViewModel(ISessionizeService sessionizeService, IFavoritesService favoritesService)
    {
        _sessionizeService = sessionizeService;
        _favoritesService = favoritesService;
        Title = "My Favorites";
        
        // Load data immediately when ViewModel is created
        _ = Task.Run(async () =>
        {
            await LoadDataAsync();
        });
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            _allSessions = await _sessionizeService.GetSessionsAsync();
            var favorites = await _favoritesService.GetFavoritesAsync();

            var favoriteSessions = _allSessions
                .Where(s => favorites.Contains(s.Id))
                .OrderBy(s => s.StartsAt)
                .ToList();

            GroupedFavorites.Clear();

            var grouped = favoriteSessions
                .GroupBy(s => new { s.StartsAt.Date, s.StartsAt.Hour, s.StartsAt.Minute })
                .Select(g =>
                {
                    var slot = new TimeSlot
                    {
                        SlotStart = g.First().StartsAt.ToString("HH:mm"),
                        StartsAt = g.First().StartsAt,
                        EndsAt = g.First().EndsAt,
                        Sessions = g.ToList()
                    };
                    return slot;
                });

            foreach (var slot in grouped)
            {
                GroupedFavorites.Add(slot);
            }

            HasFavorites = GroupedFavorites.Any();

            await ScrollToCurrentTimeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading favorites: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SessionTappedAsync(Session session)
    {
        if (session == null)
            return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Session", session }
        };

        await Shell.Current.GoToAsync($"sessiondetail", navigationParameter);
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(Session session)
    {
        if (session == null)
            return;

        await _favoritesService.RemoveFavoriteAsync(session.Id);
        await LoadDataAsync();
    }

    private async Task ScrollToCurrentTimeAsync()
    {
        await Task.Delay(100);
    }

    public List<TimeSlot> GetConflictingSlots()
    {
        return GroupedFavorites
            .Where(slot => slot.Sessions.Count > 1)
            .ToList();
    }
}
