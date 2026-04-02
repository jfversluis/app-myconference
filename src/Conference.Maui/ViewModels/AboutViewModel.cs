using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
#if IOS
using NetworkExtension;
#endif

namespace Conference.Maui.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    private readonly IEventConfigService _configService;

    public string ConferenceName => _configService.Config.Event.Name;
    public string EventDescription => _configService.Config.Event.Description;
    public string VenueName => _configService.Config.Venue.Name;
    public string VenueDetails => _configService.Config.Venue.Address;
    public bool IsPhysicalVenue => !_configService.Config.Event.IsOnline;
    public bool IsWifiEnabled => _configService.Config.Features.EnableWifi;
    public bool IsSponsorsEnabled => _configService.Config.Features.EnableSponsors;

    public string EventDateRange
    {
        get
        {
            var start = _configService.Config.Event.StartDateTime;
            var end = _configService.Config.Event.EndDateTime;

            if (start == end)
                return start.ToString("D");

            // Same month: "September 10 – 12, 2025"
            if (start.Year == end.Year && start.Month == end.Month)
                return $"{start.ToString("MMMM d")} – {end.Day}, {end.Year}";

            // Same year, different month: "September 10 – October 2, 2025"
            if (start.Year == end.Year)
                return $"{start.ToString("MMMM d")} – {end.ToString("MMMM d")}, {end.Year}";

            // Different years
            return $"{start.ToString("D")} – {end.ToString("D")}";
        }
    }

    [ObservableProperty]
    private ObservableCollection<Sponsor> _sponsors = [];

    [ObservableProperty]
    private string _wifiNetworkName = string.Empty;

    [ObservableProperty]
    private string _wifiPassword = string.Empty;

    public bool HasWifi => IsWifiEnabled && !string.IsNullOrEmpty(WifiNetworkName);

    public AboutViewModel(IEventConfigService configService)
    {
        _configService = configService;
        Title = "About";
    }

    public async Task LoadDataAsync()
    {
        if (Sponsors.Count > 0) return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("sponsors.json");
            var sponsors = await JsonSerializer.DeserializeAsync<List<Sponsor>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (sponsors != null)
            {
                Sponsors = new ObservableCollection<Sponsor>(sponsors);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading sponsors: {ex.Message}");
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("event_config.json");
            var config = await JsonSerializer.DeserializeAsync<Conference.Maui.Configuration.EventConfig>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config?.Wifi is { } wifi
                && !string.IsNullOrWhiteSpace(wifi.NetworkName))
            {
                WifiNetworkName = wifi.NetworkName;
                WifiPassword = wifi.Password;
                OnPropertyChanged(nameof(HasWifi));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading event config: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ConnectToWifiAsync()
    {
        if (string.IsNullOrEmpty(WifiNetworkName)) return;

#if IOS
        try
        {
            var config = new NEHotspotConfiguration(WifiNetworkName, WifiPassword, isWep: false);
            config.JoinOnce = false;

            var manager = new NEHotspotConfigurationManager();
            await manager.ApplyConfigurationAsync(config);
        }
        catch (Foundation.NSErrorException ex) when (ex.Error.Code == 13)
        {
            // User cancelled the system dialog — not an error
        }
        catch
        {
            // Fallback: copy password to clipboard
            await Clipboard.SetTextAsync(WifiPassword);
            await Shell.Current.DisplayAlertAsync("WiFi",
                $"Couldn't connect automatically. The password has been copied to your clipboard. " +
                $"Go to Settings → WiFi and connect to \"{WifiNetworkName}\".", "OK");
        }
#elif ANDROID
        try
        {
            var wifiManager = (Android.Net.Wifi.WifiManager?)Android.App.Application.Context
                .GetSystemService(Android.Content.Context.WifiService);

            if (wifiManager == null || Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
            {
                await CopyWifiFallbackAsync();
                return;
            }

            var suggestion = new Android.Net.Wifi.WifiNetworkSuggestion.Builder()
                .SetSsid(WifiNetworkName)
                .SetWpa2Passphrase(WifiPassword)
                .Build();

            var suggestions = new[] { suggestion };
            var status = wifiManager.AddNetworkSuggestions(suggestions);

            if (status == Android.Net.Wifi.NetworkStatus.SuggestionsSuccess)
            {
                await Shell.Current.DisplayAlertAsync("WiFi",
                    $"Network \"{WifiNetworkName}\" has been suggested. " +
                    "Your device will connect automatically when in range.", "OK");
            }
            else
            {
                await CopyWifiFallbackAsync();
            }
        }
        catch
        {
            await CopyWifiFallbackAsync();
        }
#else
        await CopyWifiFallbackAsync();
#endif
    }

    private async Task CopyWifiFallbackAsync()
    {
        await Clipboard.SetTextAsync(WifiPassword);
        await Shell.Current.DisplayAlertAsync("WiFi",
            $"The password for \"{WifiNetworkName}\" has been copied to your clipboard.", "OK");
    }

    [RelayCommand]
    private async Task OpenConferenceWebsiteAsync()
    {
        await Browser.OpenAsync(_configService.Config.Links.Website, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task OpenSessionizeAsync()
    {
        await Browser.OpenAsync(_configService.Config.Links.Sessionize, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task OpenSourceCodeAsync()
    {
        await Browser.OpenAsync(_configService.Config.Links.GitHub, BrowserLaunchMode.SystemPreferred);
    }

    public string AppVersion => $"v{Microsoft.Maui.ApplicationModel.AppInfo.VersionString} (build {Microsoft.Maui.ApplicationModel.AppInfo.BuildString})";

    [RelayCommand]
    private async Task OpenSponsorWebsiteAsync(Sponsor? sponsor)
    {
        if (sponsor == null || string.IsNullOrEmpty(sponsor.Website)) return;
        await Browser.OpenAsync(sponsor.Website, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
