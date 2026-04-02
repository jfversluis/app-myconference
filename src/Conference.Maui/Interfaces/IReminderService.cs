namespace Conference.Maui.Interfaces;

/// <summary>
/// Service for managing session reminder notifications.
/// Automatically schedules local notifications for favorited sessions.
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// Whether session reminders are globally enabled.
    /// </summary>
    bool IsGlobalRemindersEnabled { get; set; }

    /// <summary>
    /// How many minutes before a session to fire the reminder.
    /// </summary>
    int LeadTimeMinutes { get; set; }

    /// <summary>
    /// Schedules a reminder notification for a session.
    /// </summary>
    Task ScheduleReminderAsync(string sessionId, string title, string? roomName, DateTimeOffset startsAt);

    /// <summary>
    /// Cancels the reminder notification for a session.
    /// </summary>
    Task CancelReminderAsync(string sessionId);

    /// <summary>
    /// Returns whether a reminder is currently active for the given session.
    /// A reminder is active if: globally enabled, session is favorited, and not per-session disabled.
    /// </summary>
    Task<bool> IsReminderActiveAsync(string sessionId);

    /// <summary>
    /// Toggles the per-session reminder override.
    /// Returns the new reminder active state.
    /// </summary>
    Task<bool> ToggleSessionReminderAsync(string sessionId, string title, string? roomName, DateTimeOffset startsAt);

    /// <summary>
    /// Reconciles all reminders with current favorites and session data.
    /// Cancels past sessions, schedules missing reminders, fixes time changes.
    /// </summary>
    Task ReconcileRemindersAsync();
}
