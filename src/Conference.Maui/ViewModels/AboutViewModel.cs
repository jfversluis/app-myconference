using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Configuration;
using Conference.Maui.Models;
using Conference.Maui.Pages;
#if IOS
using NetworkExtension;
#endif

namespace Conference.Maui.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    public string ConferenceName => AppConfig.ConferenceName;
    public string EventDescription => AppConfig.EventDescription;
    public string EventDate => AppConfig.EventDate;
    public string VenueName => AppConfig.VenueName;
    public string VenueDetails => AppConfig.VenueDetails;

    [ObservableProperty]
    private ObservableCollection<Sponsor> _sponsors = [];

    [ObservableProperty]
    private string _wifiNetworkName = string.Empty;

    [ObservableProperty]
    private string _wifiPassword = string.Empty;

    public bool HasWifi => !string.IsNullOrEmpty(WifiNetworkName);

    public AboutViewModel()
    {
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
            var config = await JsonSerializer.DeserializeAsync<EventConfig>(stream, new JsonSerializerOptions
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
        await Browser.OpenAsync(AppConfig.ConferenceWebsite, BrowserLaunchMode.SystemPreferred);
    }

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
