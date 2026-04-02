using Conference.Maui.Models;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.Interfaces;

/// <summary>
/// Maps Sessionize API data to UI-ready SessionItem models.
/// Centralizes speaker resolution and room lookups.
/// </summary>
public interface ISessionItemMapper
{
    /// <summary>
    /// Prepares the mapper with fresh data. Call this once after loading AllDataResponse,
    /// then use MapSession for individual items. Builds internal dictionaries for O(1) lookups.
    /// </summary>
    void Initialize(AllDataResponse allData);

    /// <summary>
    /// Maps a SessionDetails to a SessionItem.
    /// </summary>
    /// <param name="session">The Sessionize session data.</param>
    /// <param name="favoriteIds">Set of favorited session IDs (null = none favorited).</param>
    /// <param name="reminderIds">Set of session IDs with active reminders (null = no reminders).</param>
    SessionItem MapSession(SessionDetails session, IReadOnlySet<string>? favoriteIds = null, IReadOnlySet<string>? reminderIds = null);

    /// <summary>
    /// Maps multiple sessions at once.
    /// </summary>
    List<SessionItem> MapSessions(IEnumerable<SessionDetails> sessions, IReadOnlySet<string>? favoriteIds = null, IReadOnlySet<string>? reminderIds = null);

    /// <summary>
    /// Resolves a room ID to a room name using the initialized data.
    /// </summary>
    string? GetRoomName(int roomId);
}
