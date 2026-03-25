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

    /// <summary>
    /// The name of the conference/event.
    /// </summary>
    public const string ConferenceName = ".NET MAUI Day 2025";

    /// <summary>
    /// A short description of the event.
    /// </summary>
    public const string EventDescription = "A full day of .NET MAUI content with world-class speakers, hands-on sessions, and community networking. Whether you're just getting started or building production apps, there's something for everyone.";

    /// <summary>
    /// Event venue name and location.
    /// </summary>
    public const string VenueName = "Online Event";
    public const string VenueDetails = "Join from anywhere — all sessions are streamed live and available on-demand after the event.";

    /// <summary>
    /// Event date display string.
    /// </summary>
    public const string EventDate = "September 10, 2025";

    /// <summary>
    /// Conference website URL.
    /// </summary>
    public const string ConferenceWebsite = "https://mauiday.net";

    /// <summary>
    /// GitHub repository URL for this open-source app.
    /// </summary>
    public const string GitHubRepo = "https://github.com/jfversluis/app-myconference";
}
