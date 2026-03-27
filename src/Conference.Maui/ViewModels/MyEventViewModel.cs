using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client.DataTransferObjects;

namespace Conference.Maui.ViewModels;

public partial class MyEventViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<MyEventViewModel> _logger;

    private const int TimerIntervalSeconds = 30;
    private const int UpNextWindowMinutes = 60;

    private AllDataResponse? _allData;
    private IReadOnlySet<string> _favoriteIds = new HashSet<string>();
    private CancellationTokenSource? _timerCts;

#if DEBUG
    // Mutable offset to simulate conference time — adjustable from the in-app debug panel.
    private static TimeSpan _debugTimeOffset = TimeSpan.Zero;

    private static readonly (string Label, DateTimeOffset Time)[] DebugPresets =
    [
        ("Before event (8:30 AM)", new(2026, 6, 1, 8, 30, 0, DateTimeOffset.Now.Offset)),
        ("Keynote live (9:30 AM)", new(2026, 6, 1, 9, 30, 0, DateTimeOffset.Now.Offset)),
        ("Break (10:05 AM)", new(2026, 6, 1, 10, 5, 0, DateTimeOffset.Now.Offset)),
        ("Sessions live (10:25 AM)", new(2026, 6, 1, 10, 25, 0, DateTimeOffset.Now.Offset)),
        ("Lunch (12:30 PM)", new(2026, 6, 1, 12, 30, 0, DateTimeOffset.Now.Offset)),
        ("Afternoon (3:00 PM)", new(2026, 6, 1, 15, 0, 0, DateTimeOffset.Now.Offset)),
        ("End of day (6:15 PM)", new(2026, 6, 1, 18, 15, 0, DateTimeOffset.Now.Offset)),
        ("Day 2 morning (9:15 AM)", new(2026, 6, 2, 9, 15, 0, DateTimeOffset.Now.Offset)),
        ("After event", new(2026, 6, 4, 10, 0, 0, DateTimeOffset.Now.Offset)),
        ("Real time (no offset)", default),
    ];
#endif

    private DateTimeOffset Now =>
#if DEBUG
        DateTimeOffset.Now + _debugTimeOffset;
#else
        DateTimeOffset.Now;
#endif

#if DEBUG
    [ObservableProperty]
    private string _debugTimeDisplay = string.Empty;

    [ObservableProperty]
    private bool _showDebugPanel;

    private int _debugPresetIndex = DebugPresets.Length - 1;

    [RelayCommand]
    private void ToggleDebugPanel() => ShowDebugPanel = !ShowDebugPanel;

    [RelayCommand]
    private void CycleDebugTime()
    {
        _debugPresetIndex = (_debugPresetIndex + 1) % DebugPresets.Length;
        var preset = DebugPresets[_debugPresetIndex];
        _debugTimeOffset = preset.Time == default ? TimeSpan.Zero : preset.Time - DateTimeOffset.Now;

        UpdateDebugTimeDisplay();
        UpdateTimeSections();
    }

    private void UpdateDebugTimeDisplay()
    {
        var now = Now;
        var preset = DebugPresets[_debugPresetIndex];
        DebugTimeDisplay = $"🐞 {now:MMM d, h:mm:ss tt} — {preset.Label}";
    }
#endif

    [ObservableProperty]
    private ObservableCollection<SessionItem> _nowSessions = [];

    [ObservableProperty]
    private ObservableCollection<SessionItem> _upNextSessions = [];

    [ObservableProperty]
    private ObservableCollection<SessionItem> _todayAgenda = [];

    [ObservableProperty]
    private string _upNextTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _countdownText = string.Empty;

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private int _totalSpeakers;

    [ObservableProperty]
    private int _agendaCount;

    [ObservableProperty]
    private string _eventDateDisplay = string.Empty;

    public string EventHeaderTitle => $"Your {AppConfig.ConferenceName}";

    [ObservableProperty]
    private bool _hasLoadError;

    [ObservableProperty]
    private string _emptyStateTitle = "No live sessions right now";

    [ObservableProperty]
    private string _emptyStateMessage = "This dashboard comes alive during the event. Explore the full schedule and add sessions to your agenda.";

    [ObservableProperty]
    private int _upNextTotalCount;

    [ObservableProperty]
    private string _wifiNetworkName = string.Empty;

    [ObservableProperty]
    private bool _hasWifi;

    public bool HasNowSessions => NowSessions.Count > 0;
    public bool HasUpNext => UpNextSessions.Count > 0;
    public bool HasTodayAgenda => TodayAgenda.Count > 0;
    public bool HasCountdown => !string.IsNullOrEmpty(CountdownText);
    public bool ShowEmptyState => !IsBusy && !HasLoadError && !HasNowSessions && !HasUpNext && !HasTodayAgenda;
    public bool ShowOnboarding => !IsBusy && !HasLoadError && AgendaCount == 0 && !ShowEmptyState;
    public bool ShowSeeAllUpNext => UpNextTotalCount > UpNextSessions.Count;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsBusy) or nameof(HasLoadError) or nameof(AgendaCount))
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowOnboarding));
        }
    }

    public MyEventViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<MyEventViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
        Title = "My Event";

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasLoadError = false;
            _allData = await _dataService.GetAllDataAsync();
            _favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();

            if (_allData != null)
            {
                var sessions = _allData.Sessions.Where(s => !s.IsServiceSession).ToList();
                TotalSessions = sessions.Count;
                TotalSpeakers = _allData.Speakers.Count;
                AgendaCount = _favoriteIds.Count;

                if (sessions.Count > 0)
                {
                    var firstDate = sessions.Min(s => s.StartsAt);
                    var lastDate = sessions.Max(s => s.EndsAt);
                    EventDateDisplay = firstDate.Year == lastDate.Year && firstDate.Month == lastDate.Month && firstDate.Day == lastDate.Day
                        ? firstDate.ToString("MMMM d, yyyy")
                        : $"{firstDate:MMM d} – {lastDate:MMM d, yyyy}";
                }

                UpdateTimeSections();
                UpdateEmptyStateText();
#if DEBUG
                UpdateDebugTimeDisplay();
                ShowDebugPanel = true;
#endif
            }

            // Load WiFi config for onboarding
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("event_config.json");
                var config = await JsonSerializer.DeserializeAsync<EventConfig>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (config?.Wifi is { } wifi && !string.IsNullOrWhiteSpace(wifi.NetworkName))
                {
                    WifiNetworkName = wifi.NetworkName;
                    HasWifi = true;
                }
            }
            catch { /* WiFi config is optional */ }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading My Event data");
            HasLoadError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void StartTimer()
    {
        StopTimer();
        _timerCts = new CancellationTokenSource();
        _ = RunTimerAsync(_timerCts.Token);
    }

    public void StopTimer()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = null;
    }

    private async Task RunTimerAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(TimerIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _favoriteIds = favoriteIds;
                        AgendaCount = _favoriteIds.Count;
                        UpdateTimeSections();
#if DEBUG
                        UpdateDebugTimeDisplay();
#endif
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Error during timer refresh, will retry next tick");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void UpdateTimeSections()
    {
        if (_allData == null) return;

        var now = Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);

        // Happening Now: sessions where StartsAt <= now < EndsAt
        // Favorites first so the user's sessions are immediately visible
        var liveSessions = _allData.Sessions
            .Where(s => !s.IsServiceSession && s.StartsAt <= now && s.EndsAt > now)
            .OrderByDescending(s => _favoriteIds.Contains(s.Id))
            .ThenBy(s => s.EndsAt)
            .Select(s => CreateSessionItem(s))
            .ToList();

        NowSessions = new ObservableCollection<SessionItem>(liveSessions);

        // Up Next: next time slot that hasn't started yet (today first, then any future date)
        var nextSlotStart = _allData.Sessions
            .Where(s => !s.IsServiceSession
                && s.StartsAt > now
                && new DateOnly(s.StartsAt.Year, s.StartsAt.Month, s.StartsAt.Day) == today)
            .Select(s => s.StartsAt)
            .Distinct()
            .OrderBy(t => t)
            .FirstOrDefault();

        // If nothing left today, look for the next day's first slot
        if (nextSlotStart == default)
        {
            nextSlotStart = _allData.Sessions
                .Where(s => !s.IsServiceSession && s.StartsAt > now)
                .Select(s => s.StartsAt)
                .Distinct()
                .OrderBy(t => t)
                .FirstOrDefault();
        }

        if (nextSlotStart != default)
        {
            var allNextSessions = _allData.Sessions
                .Where(s => !s.IsServiceSession && s.StartsAt == nextSlotStart)
                .ToList();

            UpNextTotalCount = allNextSessions.Count;

            // Show all sessions in the slot, favorites first
            var nextItems = allNextSessions
                .OrderByDescending(s => _favoriteIds.Contains(s.Id))
                .ThenBy(s => GetRoomName(s.RoomId))
                .Select(s => CreateSessionItem(s))
                .ToList();

            UpNextSessions = new ObservableCollection<SessionItem>(nextItems);

            var nextDate = new DateOnly(nextSlotStart.Year, nextSlotStart.Month, nextSlotStart.Day);
            UpNextTimeDisplay = nextDate == today
                ? nextSlotStart.ToString("h:mm tt")
                : $"Tomorrow {nextSlotStart:h:mm tt}";

            var timeUntil = nextSlotStart - now;
            CountdownText = timeUntil.TotalMinutes < 1
                ? "Starting now"
                : timeUntil.TotalMinutes < UpNextWindowMinutes
                    ? $"in {(int)timeUntil.TotalMinutes} min"
                    : $"in {(int)timeUntil.TotalHours}h {timeUntil.Minutes}m";
        }
        else
        {
            UpNextSessions = [];
            UpNextTotalCount = 0;
            UpNextTimeDisplay = string.Empty;
            CountdownText = string.Empty;
        }

        // Today's Agenda: favorited sessions for today, chronological
        var todayFavorites = _allData.Sessions
            .Where(s => !s.IsServiceSession
                && _favoriteIds.Contains(s.Id)
                && new DateOnly(s.StartsAt.Year, s.StartsAt.Month, s.StartsAt.Day) == today)
            .OrderBy(s => s.StartsAt)
            .Select(s => CreateSessionItem(s))
            .ToList();

        TodayAgenda = new ObservableCollection<SessionItem>(todayFavorites);

        OnPropertyChanged(nameof(HasNowSessions));
        OnPropertyChanged(nameof(HasUpNext));
        OnPropertyChanged(nameof(HasTodayAgenda));
        OnPropertyChanged(nameof(HasCountdown));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowOnboarding));
        OnPropertyChanged(nameof(ShowSeeAllUpNext));

        UpdateEmptyStateText();
    }

    private SessionItem CreateSessionItem(Sessionize.Api.Client.ValueObjects.SessionDetails session)
    {
        var speakers = session.Speakers
            .Select(sid => _allData?.Speakers.FirstOrDefault(s => s.Id == sid))
            .Where(s => s != null)
            .Select(s => SpeakerItem.FromSpeakerDetails(s!))
            .ToList();

        var now = Now;
        var item = new SessionItem
        {
            Id = session.Id,
            Title = session.Title,
            Description = session.Description,
            StartsAt = session.StartsAt,
            EndsAt = session.EndsAt,
            RoomId = session.RoomId,
            RoomName = GetRoomName(session.RoomId),
            Speakers = speakers,
            IsFavorite = _favoriteIds.Contains(session.Id)
        };

        return item;
    }

    private string? GetRoomName(int roomId)
    {
        return _allData?.Rooms.FirstOrDefault(r => r.Id == roomId)?.Name;
    }

    [RelayCommand]
    private async Task NavigateToSessionAsync(SessionItem session)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
        {
            ["SessionId"] = session.Id
        });
    }

    [RelayCommand]
    private async Task NavigateToSessionsAsync()
    {
        await Shell.Current.GoToAsync("//Sessions");
    }

    [RelayCommand]
    private async Task RetryLoadAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NavigateToAboutAsync()
    {
        await Shell.Current.GoToAsync("//About");
    }

    [RelayCommand]
    private async Task NavigateToQuickPickAsync()
    {
        await Shell.Current.GoToAsync(nameof(Conference.Maui.Pages.QuickPickPage));
    }

    private void UpdateEmptyStateText()
    {
        if (_allData == null || !ShowEmptyState) return;

        var now = Now.DateTime;
        var sessions = _allData.Sessions.Where(s => !s.IsServiceSession).ToList();
        if (sessions.Count == 0) return;

        var firstSession = sessions.Min(s => s.StartsAt);
        var lastSession = sessions.Max(s => s.EndsAt);

        if (now < firstSession)
        {
            // Before the event
            EmptyStateTitle = "The event hasn't started yet";
            EmptyStateMessage = $"Sessions begin at {firstSession:h:mm tt}. Browse the schedule and build your personal agenda.";
        }
        else if (now > lastSession)
        {
            // After the event
            EmptyStateTitle = "Thanks for attending!";
            EmptyStateMessage = "The event has ended. We hope you had a great time!";
        }
        else
        {
            // During the event but in a gap (break/lunch)
            var nextSession = sessions
                .Where(s => s.StartsAt > now)
                .OrderBy(s => s.StartsAt)
                .FirstOrDefault();

            if (nextSession != null)
            {
                EmptyStateTitle = "Break time";
                EmptyStateMessage = $"Next sessions start at {nextSession.StartsAt:h:mm tt}. Take a moment to explore the venue or check your agenda.";
            }
            else
            {
                EmptyStateTitle = "No live sessions right now";
                EmptyStateMessage = "This dashboard comes alive during the event. Explore the full schedule and add sessions to your agenda.";
            }
        }
    }

    public void Receive(FavoriteChangedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                _favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
                AgendaCount = _favoriteIds.Count;
                UpdateTimeSections();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling favorite change");
            }
        });
    }
}
