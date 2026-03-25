using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Configuration;

namespace Conference.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    public string AppName => AppConfig.AppName;
    public string AppVersion => $"Version {AppInfo.VersionString} (Build {AppInfo.BuildString})";
    public string Framework => ".NET MAUI";

    public SettingsViewModel()
    {
        Title = "Settings";
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
}
