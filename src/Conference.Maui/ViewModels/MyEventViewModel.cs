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

public enum EventPhase
{
    PreEvent,
    EventEve,
    DuringEvent,
    PostEvent
}

public partial class MyEventViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly IReminderService _reminderService;
    private readonly ISessionItemMapper _mapper;
    private readonly ILogger<MyEventViewModel> _logger;
    private readonly IEventConfigService _configService;

    private const int TimerIntervalSeconds = 30;
    private const int UpNextWindowMinutes = 60;

    private AllDataResponse? _allData;
    private IReadOnlySet<string> _favoriteIds = new HashSet<string>();
    private IReadOnlySet<string> _activeReminderIds = new HashSet<string>();
    private CancellationTokenSource? _timerCts;

#if DEBUG
    // Mutable offset to simulate conference time — adjustable from the in-app debug panel.
    // NDC Copenhagen 2026: sessions June 3-4. Default: real time.
    private static TimeSpan _debugTimeOffset = TimeSpan.Zero;

    private static readonly (string Label, DateTimeOffset Time)[] DebugPresets =
    [
        ("Real time (no offset)", default),
        ("Pre-event (2 weeks before)", new(2026, 5, 20, 10, 0, 0, TimeSpan.FromHours(2))),
        ("Event eve (day before)", new(2026, 6, 2, 14, 0, 0, TimeSpan.FromHours(2))),
        ("Day 1 keynote (9:15 AM)", new(2026, 6, 3, 9, 15, 0, TimeSpan.FromHours(2))),
        ("Day 1 morning (10:25 AM)", new(2026, 6, 3, 10, 25, 0, TimeSpan.FromHours(2))),
        ("Day 1 lunch (12:30 PM)", new(2026, 6, 3, 12, 30, 0, TimeSpan.FromHours(2))),
        ("Day 1 afternoon (3:05 PM)", new(2026, 6, 3, 15, 5, 0, TimeSpan.FromHours(2))),
        ("Day 1 evening (5:45 PM)", new(2026, 6, 3, 17, 45, 0, TimeSpan.FromHours(2))),
        ("Day 2 morning (10:25 AM)", new(2026, 6, 4, 10, 25, 0, TimeSpan.FromHours(2))),
        ("Day 2 afternoon (3:05 PM)", new(2026, 6, 4, 15, 5, 0, TimeSpan.FromHours(2))),
        ("After event", new(2026, 6, 4, 18, 0, 0, TimeSpan.FromHours(2))),
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

    private int _debugPresetIndex = 0;

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

    public string EventHeaderTitle => $"Your {_configService.Config.Event.Name}";
    public bool IsQuickPickEnabled => _configService.Config.Features.EnableQuickPick;

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

    // Event phase properties
    [ObservableProperty]
    private EventPhase _currentPhase;

    [ObservableProperty]
    private string _countdownDaysText = string.Empty;

    [ObservableProperty]
    private string _countdownSubtext = string.Empty;

    [ObservableProperty]
    private string _agendaSummaryText = string.Empty;

    [ObservableProperty]
    private string _venueName = string.Empty;

    [ObservableProperty]
    private string _venueAddress = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SessionItem> _firstSessions = [];

    [ObservableProperty]
    private string _firstSessionsTimeDisplay = string.Empty;

    public bool IsPreEvent => CurrentPhase == EventPhase.PreEvent;
    public bool IsEventEve => CurrentPhase == EventPhase.EventEve;
    public bool IsDuringEvent => CurrentPhase == EventPhase.DuringEvent;
    public bool IsPostEvent => CurrentPhase == EventPhase.PostEvent;
    public bool HasVenue => !string.IsNullOrEmpty(VenueName);
    public bool ShowVenueCard => HasVenue && (IsPreEvent || IsEventEve);
    public bool HasFirstSessions => FirstSessions.Count > 0 && IsEventEve;

    public bool HasNowSessions => NowSessions.Count > 0;
    public bool HasUpNext => UpNextSessions.Count > 0;
    public bool HasTodayAgenda => TodayAgenda.Count > 0;
    public bool HasCountdown => !string.IsNullOrEmpty(CountdownText);
    public bool ShowEmptyState => !IsBusy && !HasLoadError && IsDuringEvent && !HasNowSessions && !HasUpNext && !HasTodayAgenda;
    public bool ShowOnboarding => !IsBusy && !HasLoadError && AgendaCount == 0 && IsDuringEvent && !ShowEmptyState;
    public bool ShowPreEventAgenda => (IsPreEvent || IsEventEve) && AgendaCount > 0;
    public bool ShowSeeAllUpNext => UpNextTotalCount > UpNextSessions.Count;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsBusy) or nameof(HasLoadError) or nameof(AgendaCount) or nameof(CurrentPhase))
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowOnboarding));
            OnPropertyChanged(nameof(ShowPreEventAgenda));
            OnPropertyChanged(nameof(IsPreEvent));
            OnPropertyChanged(nameof(IsEventEve));
            OnPropertyChanged(nameof(IsDuringEvent));
            OnPropertyChanged(nameof(IsPostEvent));
            OnPropertyChanged(nameof(ShowVenueCard));
            OnPropertyChanged(nameof(HasFirstSessions));
        }
    }

    public MyEventViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        IReminderService reminderService,
        ISessionItemMapper mapper,
        IEventConfigService configService,
        ILogger<MyEventViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _reminderService = reminderService;
        _mapper = mapper;
        _configService = configService;
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

            // Fetch data and favorites in parallel
            var dataTask = _dataService.GetAllDataAsync();
            var favTask = _favoritesService.GetFavoriteSessionIdsAsync();
            await Task.WhenAll(dataTask, favTask);

            _allData = dataTask.Result;
            _favoriteIds = favTask.Result;
            _activeReminderIds = await _reminderService.GetActiveReminderIdsAsync(_favoriteIds);

            if (_allData != null)
            {
                _mapper.Initialize(_allData);
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
#endif
            }

            // Load WiFi and venue config
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("event_config.json");
                var config = await JsonSerializer.DeserializeAsync<Conference.Maui.Configuration.EventConfig>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (config?.Wifi is { } wifi && !string.IsNullOrWhiteSpace(wifi.NetworkName))
                {
                    WifiNetworkName = wifi.NetworkName;
                    HasWifi = true;
                }
                if (config?.Venue is { } venue && !string.IsNullOrWhiteSpace(venue.Name))
                {
                    VenueName = venue.Name;
                    VenueAddress = venue.Address;
                    OnPropertyChanged(nameof(HasVenue));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config extras are optional: {ex.Message}");
            }
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
                    var reminderIds = await _reminderService.GetActiveReminderIdsAsync(favoriteIds);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _favoriteIds = favoriteIds;
                        _activeReminderIds = reminderIds;
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
        var sessions = _allData.Sessions.Where(s => !s.IsServiceSession).ToList();

        if (sessions.Count == 0) return;

        var firstSessionStart = sessions.Min(s => s.StartsAt);
        var lastSessionEnd = sessions.Max(s => s.EndsAt);

        // Determine event phase
        var hoursUntilStart = (firstSessionStart - now).TotalHours;
        if (now > lastSessionEnd)
        {
            CurrentPhase = EventPhase.PostEvent;
        }
        else if (now >= firstSessionStart)
        {
            CurrentPhase = EventPhase.DuringEvent;
        }
        else if (hoursUntilStart <= 24)
        {
            CurrentPhase = EventPhase.EventEve;
        }
        else
        {
            CurrentPhase = EventPhase.PreEvent;
        }

        // Update phase-specific content
        UpdatePreEventContent(now, firstSessionStart);

        // Only compute live sections during the event
        if (IsDuringEvent)
        {
            UpdateLiveSections(now, today);
        }
        else
        {
            // Clear live sections for non-event phases
            NowSessions = [];
            UpNextSessions = [];
            TodayAgenda = [];
            UpNextTotalCount = 0;
            UpNextTimeDisplay = string.Empty;
            CountdownText = string.Empty;
        }

        // Update agenda summary for pre-event/eve
        AgendaSummaryText = AgendaCount > 0
            ? $"You have {AgendaCount} session{(AgendaCount == 1 ? "" : "s")} in your agenda"
            : "You haven't added any sessions yet";

        OnPropertyChanged(nameof(HasNowSessions));
        OnPropertyChanged(nameof(HasUpNext));
        OnPropertyChanged(nameof(HasTodayAgenda));
        OnPropertyChanged(nameof(HasCountdown));
        OnPropertyChanged(nameof(HasFirstSessions));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowOnboarding));
        OnPropertyChanged(nameof(ShowSeeAllUpNext));
        OnPropertyChanged(nameof(ShowPreEventAgenda));

        if (IsDuringEvent)
            UpdateEmptyStateText();
    }

    private void UpdatePreEventContent(DateTimeOffset now, DateTimeOffset firstSessionStart)
    {
        var daysUntil = (int)Math.Ceiling((firstSessionStart - now).TotalDays);
        var eventName = _configService.Config.Event.Name;

        if (IsPreEvent)
        {
            CountdownDaysText = daysUntil == 1
                ? $"1 day until {eventName}!"
                : $"{daysUntil} days until {eventName}!";
            CountdownSubtext = $"{EventDateDisplay} · {VenueName}";
        }
        else if (IsEventEve)
        {
            var hoursUntil = (int)Math.Ceiling((firstSessionStart - now).TotalHours);
            CountdownDaysText = hoursUntil <= 1
                ? "Starting soon!"
                : $"See you in {hoursUntil} hours!";
            CountdownSubtext = $"First session at {firstSessionStart.LocalDateTime:h:mm tt}";

            // Show first time slot as preview cards
            var sessions = _allData!.Sessions.Where(s => !s.IsServiceSession).ToList();
            var firstSlotStart = sessions.Min(s => s.StartsAt);
            var firstSlotSessions = sessions
                .Where(s => s.StartsAt == firstSlotStart)
                .OrderByDescending(s => _favoriteIds.Contains(s.Id))
                .ThenBy(s => _mapper.GetRoomName(s.RoomId))
                .Select(s => _mapper.MapSession(s, _favoriteIds, _activeReminderIds))
                .ToList();

            FirstSessions = new ObservableCollection<SessionItem>(firstSlotSessions);
            FirstSessionsTimeDisplay = $"{firstSlotStart:dddd, MMM d} at {firstSlotStart:h:mm tt}";
        }
        else if (IsPostEvent)
        {
            CountdownDaysText = "Thanks for attending!";
            CountdownSubtext = $"We hope you enjoyed {eventName}";
            FirstSessions = [];
        }
        else
        {
            FirstSessions = [];
        }
    }

    private void UpdateLiveSections(DateTimeOffset now, DateOnly today)
    {
        // Happening Now: sessions where StartsAt <= now < EndsAt
        var liveSessions = _allData!.Sessions
            .Where(s => !s.IsServiceSession && s.StartsAt <= now && s.EndsAt > now)
            .OrderByDescending(s => _favoriteIds.Contains(s.Id))
            .ThenBy(s => s.EndsAt)
            .Select(s => _mapper.MapSession(s, _favoriteIds, _activeReminderIds))
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

            var nextItems = allNextSessions
                .OrderByDescending(s => _favoriteIds.Contains(s.Id))
                .ThenBy(s => _mapper.GetRoomName(s.RoomId))
                .Select(s => _mapper.MapSession(s, _favoriteIds, _activeReminderIds))
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
            .Select(s => _mapper.MapSession(s, _favoriteIds, _activeReminderIds))
            .ToList();

        TodayAgenda = new ObservableCollection<SessionItem>(todayFavorites);

        UpdateEmptyStateText();
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
    private async Task RefreshDataAsync()
    {
        try
        {
            IsRefreshing = true;

            if (!await _dataService.HasDataChangedAsync())
            {
                // Data unchanged, just refresh favorites in case those changed locally
                _favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
                _activeReminderIds = await _reminderService.GetActiveReminderIdsAsync(_favoriteIds);
                AgendaCount = _favoriteIds.Count;
                UpdateTimeSections();
                _logger.LogInformation("Data unchanged, refreshed favorites only");
                return;
            }

            var dataTask = _dataService.GetAllDataAsync(forceRefresh: true);
            var favTask = _favoritesService.GetFavoriteSessionIdsAsync();
            await Task.WhenAll(dataTask, favTask);

            _allData = dataTask.Result;
            _favoriteIds = favTask.Result;
            _activeReminderIds = await _reminderService.GetActiveReminderIdsAsync(_favoriteIds);

            if (_allData != null)
            {
                _mapper.Initialize(_allData);
                var sessions = _allData.Sessions.Where(s => !s.IsServiceSession).ToList();
                TotalSessions = sessions.Count;
                TotalSpeakers = _allData.Speakers.Count;
                AgendaCount = _favoriteIds.Count;
                UpdateTimeSections();
                UpdateEmptyStateText();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing My Event data");
        }
        finally
        {
            IsRefreshing = false;
        }
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

    [RelayCommand]
    private async Task OpenInMapsAsync()
    {
        var venue = _configService.Config.Venue;
        if (venue.Latitude == 0 && venue.Longitude == 0) return;

        var location = new Location(venue.Latitude, venue.Longitude);
        var options = new MapLaunchOptions
        {
            Name = venue.Name,
            NavigationMode = NavigationMode.Default
        };

        try
        {
            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open maps app");
        }
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
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                _favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
                _activeReminderIds = await _reminderService.GetActiveReminderIdsAsync(_favoriteIds);
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
