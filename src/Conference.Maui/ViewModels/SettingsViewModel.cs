using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Conference.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private int selectedTheme = 2; // 0 = Light, 1 = Dark, 2 = System

    public SettingsViewModel()
    {
        Title = "Settings";
        LoadThemePreference();
    }

    private void LoadThemePreference()
    {
        if (Preferences.ContainsKey("AppTheme"))
        {
            SelectedTheme = Preferences.Get("AppTheme", 2);
        }
        else
        {
            SelectedTheme = 2; // System default
        }

        ApplyTheme();
    }

    partial void OnSelectedThemeChanged(int value)
    {
        Preferences.Set("AppTheme", value);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        Application.Current!.UserAppTheme = SelectedTheme switch
        {
            0 => AppTheme.Light,
            1 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    [RelayCommand]
    private async Task OpenLicensesAsync()
    {
        await Shell.Current.GoToAsync("licenses");
    }
}
