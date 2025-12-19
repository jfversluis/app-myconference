using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Conference.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private string selectedTheme = "System";

    private readonly Dictionary<string, AppTheme> _themeMap = new()
    {
        { "Light", AppTheme.Light },
        { "Dark", AppTheme.Dark },
        { "System", AppTheme.Unspecified }
    };

    public SettingsViewModel()
    {
        Title = "Settings";
        LoadThemePreference();
    }

    private void LoadThemePreference()
    {
        SelectedTheme = Preferences.Get("AppTheme", "System");
        ApplyTheme();
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Preferences.Set("AppTheme", value);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_themeMap.TryGetValue(SelectedTheme, out var theme))
        {
            Application.Current!.UserAppTheme = theme;
        }
    }

    [RelayCommand]
    private async Task OpenLicensesAsync()
    {
        await Shell.Current.GoToAsync("licenses");
    }
}
