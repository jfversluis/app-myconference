namespace Conference.Maui.Configuration;

/// <summary>
/// Central configuration for the conference app.
/// This is the single point of entry to configure the app for a specific event.
/// Event organizers should only need to modify this class to customize their app.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// The Sessionize API ID for your event.
    /// Get this from your Sessionize dashboard after enabling API/Embed access.
    /// </summary>
    public const string SessionizeApiId = "5g27052o";

    /// <summary>
    /// The base URL for the Sessionize API.
    /// Usually you don't need to change this.
    /// </summary>
    public const string SessionizeBaseUrl = "https://sessionize.com/api/v2/";

    /// <summary>
    /// App display name shown in the UI.
    /// </summary>
    public const string AppName = "MyConference";

    /// <summary>
    /// Cache duration for conference data in hours.
    /// Data will be refreshed in background but cached data will be shown immediately.
    /// </summary>
    public const int CacheExpirationHours = 24;

    /// <summary>
    /// Maximum number of retry attempts for API calls.
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Timeout for API requests in seconds.
    /// </summary>
    public const int ApiTimeoutSeconds = 30;
}
