using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Services;
using Microsoft.Maui.Accessibility;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AppleOption;

namespace Conference.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IConferenceDataService _dataService;
    private readonly IReminderService _reminderService;
    private readonly IEventConfigService _configService;
    private bool _suppressPermissionCheck;

    public string AppName => _configService.Config.App.DisplayName;
    public string AppVersion => $"Version {Microsoft.Maui.ApplicationModel.AppInfo.VersionString} (Build {Microsoft.Maui.ApplicationModel.AppInfo.BuildString})";
    public string Framework => ".NET MAUI";

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private bool _hapticFeedbackEnabled;

    [ObservableProperty]
    private bool _remindersEnabled;

    [ObservableProperty]
    private int _selectedLeadTimeIndex;

    public static int[] LeadTimeOptions => [5, 10, 15, 30];

    public SettingsViewModel(IConferenceDataService dataService, IReminderService reminderService, IEventConfigService configService)
    {
        Title = "Settings";
        _dataService = dataService;
        _reminderService = reminderService;
        _configService = configService;

        _selectedThemeIndex = Application.Current?.UserAppTheme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };

        _hapticFeedbackEnabled = HapticService.IsEnabled;
        _remindersEnabled = _reminderService.IsGlobalRemindersEnabled;

        var currentLeadTime = _reminderService.LeadTimeMinutes;
        _selectedLeadTimeIndex = Array.IndexOf(LeadTimeOptions, currentLeadTime);
        if (_selectedLeadTimeIndex < 0) _selectedLeadTimeIndex = 2;
    }

    partial void OnHapticFeedbackEnabledChanged(bool value)
    {
        HapticService.IsEnabled = value;
        SemanticScreenReader.Announce(value ? "Haptic feedback enabled" : "Haptic feedback disabled");
    }

    partial void OnRemindersEnabledChanged(bool value)
    {
        if (_suppressPermissionCheck)
            return;

        if (value)
        {
            _ = EnsureNotificationPermissionAsync();
        }
        else
        {
            _reminderService.IsGlobalRemindersEnabled = false;
            _ = _reminderService.ReconcileRemindersAsync();
            SemanticScreenReader.Announce("Reminders disabled");
        }
    }

    private async Task EnsureNotificationPermissionAsync()
    {
        try
        {
            var enabled = await LocalNotificationCenter.Current.AreNotificationsEnabled();
            if (enabled)
            {
                _reminderService.IsGlobalRemindersEnabled = true;
                _ = _reminderService.ReconcileRemindersAsync();
                SemanticScreenReader.Announce("Reminders enabled");
                return;
            }

            // Not enabled — try requesting permission (works if not yet determined)
            var permission = new NotificationPermission
            {
                Apple = new AppleNotificationPermission
                {
                    NotificationAuthorization = AppleAuthorizationOptions.Alert
                        | AppleAuthorizationOptions.Badge
                        | AppleAuthorizationOptions.Sound
                        | AppleAuthorizationOptions.TimeSensitive
                }
            };

            var granted = await LocalNotificationCenter.Current.RequestNotificationPermission(permission);
            if (granted)
            {
                _reminderService.IsGlobalRemindersEnabled = true;
                _ = _reminderService.ReconcileRemindersAsync();
                SemanticScreenReader.Announce("Reminders enabled");
                return;
            }

            // Permission denied — on iOS the system won't re-prompt, send to Settings
            _suppressPermissionCheck = true;
            RemindersEnabled = false;
            _suppressPermissionCheck = false;

            var openSettings = await Shell.Current.DisplayAlertAsync(
                "Notifications Disabled",
                "Session reminders require notification permission. Would you like to open Settings to enable them?",
                "Open Settings", "Cancel");

            if (openSettings)
                Microsoft.Maui.ApplicationModel.AppInfo.ShowSettingsUI();
        }
        catch
        {
            _suppressPermissionCheck = true;
            RemindersEnabled = false;
            _suppressPermissionCheck = false;
        }
    }

    partial void OnSelectedLeadTimeIndexChanged(int value)
    {
        if (value >= 0 && value < LeadTimeOptions.Length)
        {
            _reminderService.LeadTimeMinutes = LeadTimeOptions[value];
            if (_reminderService.IsGlobalRemindersEnabled)
                _ = _reminderService.ReconcileRemindersAsync();
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (Application.Current is null) return;

        Application.Current.UserAppTheme = value switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        Preferences.Set(PreferenceKeys.AppTheme, value);
    }

    [RelayCommand]
    private void SetTheme(string? indexStr)
    {
        if (int.TryParse(indexStr, out var index))
        {
            SelectedThemeIndex = index;
            var names = new[] { "System", "Light", "Dark" };
            SemanticScreenReader.Announce($"Theme set to {names[index]}");
        }
    }

    [RelayCommand]
    private void SetLeadTime(string? indexStr)
    {
        if (int.TryParse(indexStr, out var index))
        {
            SelectedLeadTimeIndex = index;
            var times = new[] { "5 minutes", "10 minutes", "15 minutes", "30 minutes" };
            SemanticScreenReader.Announce($"Reminder lead time set to {times[index]}");
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Clear Cache",
            "This will remove all cached conference data. Fresh data will be downloaded next time.",
            "Clear", "Cancel");

        if (!confirm) return;

        await _dataService.ClearCacheAsync();
        SemanticScreenReader.Announce("Cache cleared successfully");
        await Shell.Current.DisplayAlertAsync("Done", "Cache cleared successfully.", "OK");
    }

    [RelayCommand]
    private async Task ReplayOnboardingAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Replay Welcome",
            "This will show the welcome experience again next time you open the app.",
            "Replay", "Cancel");

        if (!confirm) return;

        OnboardingViewModel.ResetOnboarding();
        await Shell.Current.DisplayAlertAsync("Done", "The welcome experience will show when you restart the app.", "OK");
    }

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Browser.OpenAsync(_configService.Config.Links.GitHub, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task OpenSessionizeAsync()
    {
        await Browser.OpenAsync(_configService.Config.Links.Sessionize, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private static async Task OpenLibraryAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }
}
