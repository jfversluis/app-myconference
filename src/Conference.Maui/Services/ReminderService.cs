using System.Reactive.Linq;
using Akavache;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AppleOption;

namespace Conference.Maui.Services;

public class ReminderService : IReminderService, IRecipient<FavoriteChangedMessage>
{
    private readonly IFavoritesService _favoritesService;
    private readonly IConferenceDataService _dataService;
    private readonly ILogger<ReminderService> _logger;
    private readonly IBlobCache _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Dictionary<string, bool>? _overrides;

    private const string OverridesCacheKey = "session_reminder_overrides";
    private const int DefaultLeadTimeMinutes = 15;

    public ReminderService(
        IFavoritesService favoritesService,
        IConferenceDataService dataService,
        ILogger<ReminderService> logger)
    {
        _favoritesService = favoritesService;
        _dataService = dataService;
        _logger = logger;
        _cache = BlobCache.UserAccount;

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    public bool IsGlobalRemindersEnabled
    {
        get => Preferences.Get(PreferenceKeys.RemindersEnabled, true);
        set => Preferences.Set(PreferenceKeys.RemindersEnabled, value);
    }

    public int LeadTimeMinutes
    {
        get => Preferences.Get(PreferenceKeys.ReminderLeadTimeMinutes, DefaultLeadTimeMinutes);
        set => Preferences.Set(PreferenceKeys.ReminderLeadTimeMinutes, value);
    }

    public async Task ScheduleReminderAsync(string sessionId, string title, string? roomName, DateTimeOffset startsAt)
    {
        if (!IsGlobalRemindersEnabled)
            return;

        if (await IsSessionOverriddenOffAsync(sessionId))
            return;

        var notifyTime = startsAt.LocalDateTime.AddMinutes(-LeadTimeMinutes);
        var now = DateTime.Now;

#if DEBUG
        if (notifyTime <= now)
        {
            // In debug, schedule 5 seconds from now so we can test notifications with past data
            notifyTime = now.AddSeconds(5);
            _logger.LogDebug("DEBUG: Rescheduling past reminder for {SessionId} to fire in 5s", sessionId);
        }
#else
        if (notifyTime <= now)
        {
            _logger.LogDebug("Skipping reminder for {SessionId}: notify time {NotifyTime} is in the past", sessionId, notifyTime);
            return;
        }
#endif

        var notificationId = GetNotificationId(sessionId);
        var roomText = string.IsNullOrEmpty(roomName) ? "" : $" • {roomName}";
        var minutesText = LeadTimeMinutes == 1 ? "1 min" : $"{LeadTimeMinutes} min";

        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            Title = "Starting Soon",
            Description = $"{title}{roomText} · Starts in {minutesText}",
            ReturningData = sessionId,
            CategoryType = NotificationCategoryType.Reminder,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime
            },
            Apple = new AppleOptions
            {
                Priority = ApplePriority.TimeSensitive,
                RelevanceScore = 1.0
            }
        };

        await LocalNotificationCenter.Current.Show(request);
        _logger.LogDebug("Scheduled reminder for {SessionId} at {NotifyTime}", sessionId, notifyTime);
    }

    public async Task CancelReminderAsync(string sessionId)
    {
        var notificationId = GetNotificationId(sessionId);
        LocalNotificationCenter.Current.Cancel(notificationId);
        _logger.LogDebug("Cancelled reminder for {SessionId}", sessionId);

        // Clean up override when unfavoriting
        await RemoveOverrideAsync(sessionId);
    }

    public async Task<bool> IsReminderActiveAsync(string sessionId)
    {
        if (!IsGlobalRemindersEnabled)
            return false;

        return !await IsSessionOverriddenOffAsync(sessionId);
    }

    public async Task<IReadOnlySet<string>> GetActiveReminderIdsAsync(IEnumerable<string> sessionIds)
    {
        if (!IsGlobalRemindersEnabled)
            return new HashSet<string>();

        var result = new HashSet<string>();
        foreach (var id in sessionIds)
        {
            if (!await IsSessionOverriddenOffAsync(id))
                result.Add(id);
        }
        return result;
    }

    public async Task<bool> ToggleSessionReminderAsync(string sessionId, string title, string? roomName, DateTimeOffset startsAt)
    {
        var isCurrentlyOff = await IsSessionOverriddenOffAsync(sessionId);

        if (isCurrentlyOff)
        {
            // Turn reminder back on — remove the override
            await SetOverrideAsync(sessionId, true);
            await ScheduleReminderAsync(sessionId, title, roomName, startsAt);
            return true;
        }
        else
        {
            // Turn reminder off — set override to false
            await SetOverrideAsync(sessionId, false);
            var notificationId = GetNotificationId(sessionId);
            LocalNotificationCenter.Current.Cancel(notificationId);
            return false;
        }
    }

    public async Task ReconcileRemindersAsync()
    {
        try
        {
            var favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
            var allData = await _dataService.GetAllDataAsync();

            if (allData?.Sessions == null || favoriteIds.Count == 0)
            {
                _logger.LogDebug("No data or no favorites — clearing all reminders");
                LocalNotificationCenter.Current.CancelAll();
                return;
            }

            var sessionMap = allData.Sessions.ToDictionary(s => s.Id);
            var now = DateTimeOffset.Now;

            // Cancel all first, then reschedule active ones — simplest reconciliation
            LocalNotificationCenter.Current.CancelAll();

            if (!IsGlobalRemindersEnabled)
            {
                _logger.LogDebug("Global reminders disabled — all cancelled");
                return;
            }

            var scheduled = 0;
            foreach (var sessionId in favoriteIds)
            {
                if (!sessionMap.TryGetValue(sessionId, out var session))
                    continue;

                if (session.StartsAt <= now)
                    continue; // Past session

                if (await IsSessionOverriddenOffAsync(sessionId))
                    continue;

                var roomName = session.RoomId > 0 && allData.Rooms != null
                    ? allData.Rooms.FirstOrDefault(r => r.Id == session.RoomId)?.Name
                    : null;

                await ScheduleReminderInternalAsync(sessionId, session.Title, roomName, session.StartsAt);
                scheduled++;
            }

            _logger.LogInformation("Reconciled reminders: {Scheduled} scheduled for {Total} favorites", scheduled, favoriteIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling reminders");
        }
    }

    public void Receive(FavoriteChangedMessage message)
    {
        // Fire-and-forget — handle on background thread
        _ = Task.Run(async () =>
        {
            try
            {
                if (message.IsFavorite)
                {
                    await HandleSessionFavoritedAsync(message.SessionId);
                }
                else
                {
                    await CancelReminderAsync(message.SessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling favorite change for {SessionId}", message.SessionId);
            }
        });
    }

    private async Task HandleSessionFavoritedAsync(string sessionId)
    {
        if (!IsGlobalRemindersEnabled)
            return;

        var allData = await _dataService.GetAllDataAsync();
        var session = allData?.Sessions?.FirstOrDefault(s => s.Id == sessionId);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found in data", sessionId);
            return;
        }

        var roomName = session.RoomId > 0 && allData?.Rooms != null
            ? allData.Rooms.FirstOrDefault(r => r.Id == session.RoomId)?.Name
            : null;

        await ScheduleReminderAsync(sessionId, session.Title, roomName, session.StartsAt);
    }

    /// <summary>
    /// Internal schedule that bypasses the global/override checks (used during reconciliation).
    /// </summary>
    private async Task ScheduleReminderInternalAsync(string sessionId, string title, string? roomName, DateTimeOffset startsAt)
    {
        var notifyTime = startsAt.LocalDateTime.AddMinutes(-LeadTimeMinutes);
        var now = DateTime.Now;

#if DEBUG
        // In debug, don't bulk-schedule past sessions on reconciliation — only on-demand via ScheduleReminderAsync
        if (notifyTime <= now)
            return;
#else
        if (notifyTime <= now)
            return;
#endif

        var notificationId = GetNotificationId(sessionId);
        var roomText = string.IsNullOrEmpty(roomName) ? "" : $" • {roomName}";
        var minutesText = LeadTimeMinutes == 1 ? "1 min" : $"{LeadTimeMinutes} min";

        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            Title = "Starting Soon",
            Description = $"{title}{roomText} · Starts in {minutesText}",
            ReturningData = sessionId,
            CategoryType = NotificationCategoryType.Reminder,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime
            },
            Apple = new AppleOptions
            {
                Priority = ApplePriority.TimeSensitive,
                RelevanceScore = 1.0
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    #region Per-Session Overrides

    private async Task EnsureOverridesLoadedAsync()
    {
        if (_overrides != null) return;

        try
        {
            _overrides = await _cache.GetObject<Dictionary<string, bool>>(OverridesCacheKey);
            _logger.LogDebug("Loaded {Count} reminder overrides from cache", _overrides.Count);
        }
        catch (KeyNotFoundException)
        {
            _overrides = [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reminder overrides");
            _overrides = [];
        }
    }

    private async Task<bool> IsSessionOverriddenOffAsync(string sessionId)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureOverridesLoadedAsync();
            return _overrides!.TryGetValue(sessionId, out var enabled) && !enabled;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SetOverrideAsync(string sessionId, bool enabled)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureOverridesLoadedAsync();

            if (enabled)
            {
                // Removing override = back to default (reminder ON for favorites)
                _overrides!.Remove(sessionId);
            }
            else
            {
                _overrides![sessionId] = false;
            }

            await SaveOverridesAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RemoveOverrideAsync(string sessionId)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureOverridesLoadedAsync();
            if (_overrides!.Remove(sessionId))
            {
                await SaveOverridesAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveOverridesAsync()
    {
        try
        {
            await _cache.InsertObject(OverridesCacheKey, _overrides!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reminder overrides");
        }
    }

    #endregion

    private static int GetNotificationId(string sessionId)
    {
        // Deterministic hash — string.GetHashCode() is randomized per-process in .NET
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in sessionId)
            {
                hash = (hash ^ c) * 16777619;
            }
            return hash & 0x7FFFFFFF; // Ensure positive
        }
    }
}
