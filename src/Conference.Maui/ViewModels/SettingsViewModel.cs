using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;

namespace Conference.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IConferenceDataService _dataService;

    public string AppName => AppConfig.AppName;
    public string AppVersion => $"Version {AppInfo.VersionString} (Build {AppInfo.BuildString})";
    public string Framework => ".NET MAUI";

    [ObservableProperty]
    private int _selectedThemeIndex;

    public SettingsViewModel(IConferenceDataService dataService)
    {
        Title = "Settings";
        _dataService = dataService;

        _selectedThemeIndex = Application.Current?.UserAppTheme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };
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

        Preferences.Set("app_theme", value);
    }

    [RelayCommand]
    private void SetTheme(string? indexStr)
    {
        if (int.TryParse(indexStr, out var index))
            SelectedThemeIndex = index;
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
        await Shell.Current.DisplayAlertAsync("Done", "Cache cleared successfully.", "OK");
    }

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Browser.OpenAsync(AppConfig.GitHubRepo, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task OpenSessionizeAsync()
    {
        await Browser.OpenAsync("https://sessionize.com", BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private static async Task OpenLibraryAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }
}
