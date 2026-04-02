using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Maui.Extensions;
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

    /// <summary>
    /// Loads config synchronously — safe to call from CreateWindow on the main thread.
    /// On iOS/Mac Catalyst, reads directly from the bundle directory.
    /// On Android, MauiAsset files live inside the APK and must be read via Assets.Open().
    /// </summary>
    public void Initialize()
    {
        try
        {
            string? json = null;

#if ANDROID
            using var stream = Android.App.Application.Context.Assets!.Open("event_config.json");
            using var reader = new StreamReader(stream);
            json = reader.ReadToEnd();
#else
            var path = Path.Combine(AppContext.BaseDirectory, "event_config.json");
            if (File.Exists(path))
                json = File.ReadAllText(path);
#endif

            if (json is not null)
            {
                var config = JsonSerializer.Deserialize<EventConfig>(json, s_jsonOptions);
                if (config is not null)
                    Config = config;
            }
            else
            {
                Debug.WriteLine("[EventConfigService] event_config.json not found, using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EventConfigService] Failed to load event_config.json: {ex.Message}");
        }
    }

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

    /// <summary>
    /// Applies branding colors from config to the app's resource dictionary.
    /// Must be called after InitializeAsync and before any pages are created.
    /// </summary>
    public void ApplyBranding(ResourceDictionary resources)
    {
        var branding = Config.Branding;
        if (branding is null) return;

        TrySetColor(resources, "Primary", branding.PrimaryColor);
        TrySetColor(resources, "PrimaryDark", branding.PrimaryDark);
        TrySetColor(resources, "PrimaryLight", branding.PrimaryLight);
        TrySetColor(resources, "PrimaryDeep", branding.PrimaryDeep);
        TrySetColor(resources, "LightPrimary", branding.PrimaryColor);
        TrySetColor(resources, "Secondary", branding.SecondaryColor);
        TrySetColor(resources, "SecondaryDark", branding.SecondaryColor);
        TrySetColor(resources, "Accent", branding.AccentColor);

        // Auto-generate a lighter dark-mode primary from the brand color
        if (branding.PrimaryColor is not null)
        {
            var primaryColor = TryParseColor(branding.PrimaryColor);
            if (primaryColor is not null)
            {
                var darkPrimary = primaryColor.WithLuminosity(
                    Math.Min(primaryColor.GetLuminosity() + 0.15f, 0.85f));
                resources["DarkPrimary"] = darkPrimary;
            }
        }

        // Update hero gradient if custom colors specified
        if (branding.HeroGradientStart is not null ||
            branding.HeroGradientMiddle is not null ||
            branding.HeroGradientEnd is not null)
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(TryParseColor(branding.HeroGradientStart) ?? Color.FromArgb("#005A9E"), 0.0f),
                    new GradientStop(TryParseColor(branding.HeroGradientMiddle) ?? Color.FromArgb("#0078D4"), 0.5f),
                    new GradientStop(TryParseColor(branding.HeroGradientEnd) ?? Color.FromArgb("#1E70C0"), 1.0f)
                }
            };
            resources["HeroGradient"] = gradient;
        }

        // Update AppThemeColor objects that captured old Color references at XAML parse time
        UpdateAppThemeColor(resources, "ThemePrimary",
            TryParseColor(branding.PrimaryColor),
            branding.PrimaryColor is not null ? TryParseColor(branding.PrimaryColor)?.WithLuminosity(
                Math.Min((TryParseColor(branding.PrimaryColor)?.GetLuminosity() ?? 0.5f) + 0.15f, 0.85f)) : null);
    }

    private static void UpdateAppThemeColor(ResourceDictionary resources, string key, Color? light, Color? dark)
    {
        // Search merged dictionaries for the AppThemeColor
        if (TryFindResource(resources, key) is CommunityToolkit.Maui.AppThemeColor themeColor)
        {
            if (light is not null) themeColor.Light = light;
            if (dark is not null) themeColor.Dark = dark;
        }
    }

    private static object? TryFindResource(ResourceDictionary resources, string key)
    {
        if (resources.TryGetValue(key, out var value))
            return value;

        foreach (var merged in resources.MergedDictionaries)
        {
            var found = TryFindResource(merged, key);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static void TrySetColor(ResourceDictionary resources, string key, string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return;
        try
        {
            resources[key] = Color.FromArgb(hex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EventConfigService] Invalid color '{hex}' for {key}: {ex.Message}");
        }
    }

    private static Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return null;
        try { return Color.FromArgb(hex); }
        catch { return null; }
    }
}
