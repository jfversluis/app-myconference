using System.Text.Json.Serialization;

namespace Conference.Maui.Configuration;

/// <summary>
/// Root configuration model loaded from event_config.json.
/// This is the single file event organizers modify to white-label the app.
/// </summary>
public sealed class EventConfig
{
    [JsonPropertyName("event")]
    public EventInfo Event { get; set; } = new();

    [JsonPropertyName("venue")]
    public VenueInfo Venue { get; set; } = new();

    [JsonPropertyName("api")]
    public ApiConfig Api { get; set; } = new();

    [JsonPropertyName("app")]
    public AppInfo App { get; set; } = new();

    [JsonPropertyName("links")]
    public LinksConfig Links { get; set; } = new();

    [JsonPropertyName("wifi")]
    public WifiConfig Wifi { get; set; } = new();

    [JsonPropertyName("branding")]
    public BrandingConfig Branding { get; set; } = new();

    [JsonPropertyName("features")]
    public FeatureFlags Features { get; set; } = new();
}

public sealed class EventInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "My Conference";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    public DateTime StartDateTime => DateTime.TryParse(StartDate, out var d) ? d : DateTime.Today;
    public DateTime EndDateTime => DateTime.TryParse(EndDate, out var d) ? d : DateTime.Today;
}

public sealed class VenueInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public sealed class ApiConfig
{
    [JsonPropertyName("sessionizeEventId")]
    public string SessionizeEventId { get; set; } = string.Empty;

    [JsonPropertyName("sessionizeBaseUrl")]
    public string SessionizeBaseUrl { get; set; } = "https://sessionize.com/api/v2/";

    [JsonPropertyName("cacheExpirationHours")]
    public int CacheExpirationHours { get; set; } = 24;

    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    [JsonPropertyName("apiTimeoutSeconds")]
    public int ApiTimeoutSeconds { get; set; } = 30;
}

public sealed class AppInfo
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "MyConference";

    [JsonPropertyName("bundleId")]
    public string BundleId { get; set; } = "com.myconference.app";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}

public sealed class LinksConfig
{
    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

    [JsonPropertyName("github")]
    public string GitHub { get; set; } = string.Empty;

    [JsonPropertyName("sessionize")]
    public string Sessionize { get; set; } = "https://sessionize.com";
}

public sealed class WifiConfig
{
    [JsonPropertyName("networkName")]
    public string NetworkName { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class FeatureFlags
{
    [JsonPropertyName("enableQuickPick")]
    public bool EnableQuickPick { get; set; } = true;

    [JsonPropertyName("enableOnboarding")]
    public bool EnableOnboarding { get; set; } = true;

    [JsonPropertyName("enableReminders")]
    public bool EnableReminders { get; set; } = true;

    [JsonPropertyName("enableWifi")]
    public bool EnableWifi { get; set; } = true;

    [JsonPropertyName("enableSponsors")]
    public bool EnableSponsors { get; set; } = true;

    [JsonPropertyName("enableConflictResolver")]
    public bool EnableConflictResolver { get; set; } = true;

    [JsonPropertyName("enableHapticFeedback")]
    public bool EnableHapticFeedback { get; set; } = true;
}

public sealed class BrandingConfig
{
    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("primaryDark")]
    public string? PrimaryDark { get; set; }

    [JsonPropertyName("primaryLight")]
    public string? PrimaryLight { get; set; }

    [JsonPropertyName("primaryDeep")]
    public string? PrimaryDeep { get; set; }

    [JsonPropertyName("secondaryColor")]
    public string? SecondaryColor { get; set; }

    [JsonPropertyName("accentColor")]
    public string? AccentColor { get; set; }

    [JsonPropertyName("heroGradientStart")]
    public string? HeroGradientStart { get; set; }

    [JsonPropertyName("heroGradientMiddle")]
    public string? HeroGradientMiddle { get; set; }

    [JsonPropertyName("heroGradientEnd")]
    public string? HeroGradientEnd { get; set; }
}
