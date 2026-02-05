using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class SessionsViewModel : BaseViewModel
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<SessionsViewModel> _logger;

    private AllDataResponse? _allData;
    private List<ScheduleDay> _allDays = [];

    [ObservableProperty]
    private ObservableCollection<ScheduleDay> _days = [];

    [ObservableProperty]
    private ScheduleDay? _selectedDay;

    [ObservableProperty]
    private int _selectedDayIndex;

    [ObservableProperty]
    private ObservableCollection<TimeSlotGroup> _currentDaySlots = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SessionsViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<SessionsViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
        Title = "Sessions";

        _favoritesService.FavoritesChanged += OnFavoritesChanged;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            _logger.LogInformation("Loading sessions data");

            _allData = await _dataService.GetAllDataAsync();
            
            if (_allData == null)
            {
                _logger.LogWarning("No data received from service");
                return;
            }

            await ProcessDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sessions");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        IsRefreshing = true;
        _allData = await _dataService.GetAllDataAsync(forceRefresh: true);
        
        if (_allData != null)
        {
            await ProcessDataAsync();
        }
        
        IsRefreshing = false;
    }

    private async Task ProcessDataAsync()
    {
        if (_allData == null) return;

        var favorites = await _favoritesService.GetFavoriteSessionIdsAsync();

        // Group sessions by date
        var sessionsByDate = _allData.Sessions
            .GroupBy(s => 
            {
                // StartsAt is DateTimeOffset - extract the DateTime part
                var dt = s.StartsAt;
                return new DateOnly(dt.Year, dt.Month, dt.Day);
            })
            .OrderBy(g => g.Key)
            .ToList();

        _allDays.Clear();
        
        foreach (var dateGroup in sessionsByDate)
        {
            var scheduleDay = new ScheduleDay { Date = dateGroup.Key };

            // Group by time slot
            var timeSlots = dateGroup
                .GroupBy(s => (s.StartsAt, s.EndsAt))
                .OrderBy(g => g.Key.StartsAt)
                .Select(slotGroup =>
                {
                    var slot = new TimeSlotGroup
                    {
                        StartTime = slotGroup.Key.StartsAt,
                        EndTime = slotGroup.Key.EndsAt
                    };
                    
                    foreach (var session in slotGroup.OrderBy(s => GetRoomName(s.RoomId)))
                    {
                        slot.Add(CreateSessionItem(session, favorites));
                    }
                    
                    return slot;
                })
                .ToList();

            scheduleDay.TimeSlots = timeSlots;
            _allDays.Add(scheduleDay);
        }

        Days = new ObservableCollection<ScheduleDay>(_allDays);

        // Select appropriate day (today if within event dates, otherwise first day)
        SelectAppropriateDay();
    }

    private SessionItem CreateSessionItem(SessionDetails session, HashSet<string> favorites)
    {
        var speakers = session.Speakers
            .Select(speakerId => _allData?.Speakers.FirstOrDefault(s => s.Id == speakerId))
            .Where(s => s != null)
            .Select(s => SpeakerItem.FromSpeakerDetails(s!))
            .ToList();

        return new SessionItem
        {
            Id = session.Id,
            Title = session.Title,
            Description = session.Description,
            StartsAt = session.StartsAt,
            EndsAt = session.EndsAt,
            RoomId = session.RoomId,
            RoomName = GetRoomName(session.RoomId),
            Speakers = speakers,
            IsFavorite = favorites.Contains(session.Id)
        };
    }

    private string? GetRoomName(int roomId)
    {
        return _allData?.Rooms.FirstOrDefault(r => r.Id == roomId)?.Name;
    }

    private void SelectAppropriateDay()
    {
        if (Days.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var matchingDay = Days.FirstOrDefault(d => d.Date == today) ?? Days.First();
        
        // Set the initial selection
        matchingDay.IsSelected = true;
        SelectedDay = matchingDay;
    }

    partial void OnSelectedDayChanged(ScheduleDay? value)
    {
        if (value == null) return;
        
        ApplySearch();
    }

    partial void OnSelectedDayIndexChanged(int value)
    {
        if (Days.Count > value && value >= 0)
        {
            SelectDay(Days[value]);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearch();
    }

    private void ApplySearch()
    {
        if (SelectedDay == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            CurrentDaySlots = new ObservableCollection<TimeSlotGroup>(SelectedDay.TimeSlots);
            return;
        }

        var searchLower = SearchText.ToLowerInvariant();
        var filteredSlots = SelectedDay.TimeSlots
            .Select(slot =>
            {
                var filteredSlot = new TimeSlotGroup
                {
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime
                };
                
                foreach (var session in slot.Where(s =>
                    s.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                    s.Speakers.Any(sp => sp.FullName.Contains(searchLower, StringComparison.OrdinalIgnoreCase)) ||
                    (s.RoomName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    filteredSlot.Add(session);
                }
                
                return filteredSlot;
            })
            .Where(slot => slot.Count > 0)
            .ToList();

        CurrentDaySlots = new ObservableCollection<TimeSlotGroup>(filteredSlots);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(SessionItem session)
    {
        session.IsFavorite = await _favoritesService.ToggleFavoriteAsync(session.Id);
    }

    [RelayCommand]
    private async Task NavigateToSessionDetailsAsync(SessionItem session)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
        {
            ["SessionId"] = session.Id
        });
    }

    [RelayCommand]
    private void SelectDay(ScheduleDay day)
    {
        // Clear all selections first
        foreach (var d in Days)
        {
            d.IsSelected = false;
        }
        
        // Select the new day
        day.IsSelected = true;
        SelectedDay = day;
    }

    private void OnFavoritesChanged(object? sender, string sessionId)
    {
        // Update favorite status in current view
        foreach (var slot in CurrentDaySlots)
        {
            var session = slot.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.IsFavorite = !session.IsFavorite;
            }
        }
    }
}
