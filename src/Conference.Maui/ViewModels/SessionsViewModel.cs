using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Services;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class SessionsViewModel : BaseViewModel
{
    private readonly ISessionizeService _sessionizeService;
    private readonly IFavoritesService _favoritesService;
    private List<DaySchedule> _allSchedule = new();
    private List<Session> _allSessions = new();

    [ObservableProperty]
    private ObservableCollection<GroupedSessions> groupedSessions = new();

    [ObservableProperty]
    private ObservableCollection<DaySchedule> days = new();

    [ObservableProperty]
    private DaySchedule? selectedDay;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    public SessionsViewModel(ISessionizeService sessionizeService, IFavoritesService favoritesService)
    {
        _sessionizeService = sessionizeService;
        _favoritesService = favoritesService;
        Title = "Sessions";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync(false);
    }

    [RelayCommand]
    private async Task LoadDataAsync(bool forceRefresh)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            _allSchedule = await _sessionizeService.GetScheduleAsync(forceRefresh);
            _allSessions = _allSchedule
                .SelectMany(d => d.TimeSlots)
                .SelectMany(ts => ts.Sessions)
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            Days.Clear();
            foreach (var day in _allSchedule)
            {
                Days.Add(day);
            }

            if (SelectedDay == null && Days.Any())
            {
                var today = DateTime.Today;
                var matchingDay = Days.FirstOrDefault(d => d.Date.Date == today);
                SelectedDay = matchingDay ?? Days.First();
            }
            else if (SelectedDay != null)
            {
                var updatedDay = Days.FirstOrDefault(d => d.Date.Date == SelectedDay.Date.Date);
                if (updatedDay != null)
                {
                    SelectedDay = updatedDay;
                }
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading sessions: {ex.Message}");
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
        await LoadDataAsync(true);
    }

    partial void OnSelectedDayChanged(DaySchedule? value)
    {
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        GroupedSessions.Clear();

        if (SelectedDay == null)
            return;

        var filteredSlots = SelectedDay.TimeSlots.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredSlots = filteredSlots
                .Select(slot => new TimeSlot
                {
                    SlotStart = slot.SlotStart,
                    StartsAt = slot.StartsAt,
                    EndsAt = slot.EndsAt,
                    Sessions = slot.Sessions
                        .Where(s => s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                   s.Speakers.Any(sp => sp.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                        .ToList()
                })
                .Where(slot => slot.Sessions.Any());
        }

        foreach (var slot in filteredSlots)
        {
            var grouped = new GroupedSessions(slot);
            GroupedSessions.Add(grouped);
        }
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
    private async Task ToggleFavoriteAsync(Session session)
    {
        if (session == null)
            return;

        var isFavorite = await _favoritesService.IsFavoriteAsync(session.Id);
        
        if (isFavorite)
        {
            await _favoritesService.RemoveFavoriteAsync(session.Id);
        }
        else
        {
            await _favoritesService.AddFavoriteAsync(session.Id);
        }
    }
}
