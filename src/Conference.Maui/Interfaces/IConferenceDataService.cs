using Sessionize.Api.Client.DataTransferObjects;

namespace Conference.Maui.Interfaces;

/// <summary>
/// Service for fetching and caching conference data from Sessionize.
/// </summary>
public interface IConferenceDataService
{
    /// <summary>
    /// Gets all conference data (sessions, speakers, rooms, etc.).
    /// Returns cached data immediately if available, then refreshes in background.
    /// </summary>
    Task<AllDataResponse?> GetAllDataAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the schedule grid organized by day and room.
    /// </summary>
    Task<List<ScheduleGridResponse>> GetScheduleGridAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the data has changed on the server using the hash endpoint.
    /// </summary>
    Task<bool> HasDataChangedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    Task ClearCacheAsync();
}
