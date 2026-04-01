using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.ViewModels;

namespace Conference.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var savedTheme = Preferences.Get("app_theme", 0);
        UserAppTheme = savedTheme switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var sp = IPlatformApplication.Current?.Services;

        // Preload conference data in background so it's ready when pages appear
        _ = Task.Run(async () =>
        {
            try
            {
                var dataService = sp?.GetService<IConferenceDataService>();
                if (dataService != null)
                    await dataService.GetAllDataAsync();
            }
            catch { /* Non-critical preload */ }
        });

        if (OnboardingViewModel.IsOnboardingCompleted())
            return new Window(new AppShell());

        var onboardingPage = sp!.GetRequiredService<OnboardingPage>();
        return new Window(onboardingPage);
    }

    /// <summary>
    /// Transitions from onboarding to the main Shell.
    /// </summary>
    public static void TransitionToShell()
    {
        if (Current?.Windows.FirstOrDefault() is Window window)
            window.Page = new AppShell();
    }

    /// <summary>
    /// Transitions from onboarding to the main Shell, navigating to a specific route.
    /// </summary>
    public static async void TransitionToShell(string route)
    {
        if (Current?.Windows.FirstOrDefault() is Window window)
        {
            window.Page = new AppShell();
            await Task.Delay(300); // Let Shell initialize
            await Shell.Current.GoToAsync(route);
        }
    }
}
