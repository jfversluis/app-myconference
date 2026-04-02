using System.Diagnostics;
using System.Text.Json;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;

namespace Conference.Maui.Services;

/// <summary>
/// Loads and provides the event configuration from the bundled event_config.json.
/// Registered as a singleton and initialized at app startup.
/// </summary>
public sealed class EventConfigService : IEventConfigService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EventConfig Config { get; private set; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("event_config.json");
            var config = await JsonSerializer.DeserializeAsync<EventConfig>(stream, s_jsonOptions);
            if (config is not null)
                Config = config;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EventConfigService] Failed to load event_config.json: {ex.Message}");
        }
    }
}
