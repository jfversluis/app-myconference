using Conference.Maui.Configuration;

namespace Conference.Maui.Interfaces;

/// <summary>
/// Provides access to the event configuration loaded from event_config.json.
/// Replaces the static AppConfig class with a runtime-configurable alternative.
/// </summary>
public interface IEventConfigService
{
    EventConfig Config { get; }
    Task InitializeAsync();
}
