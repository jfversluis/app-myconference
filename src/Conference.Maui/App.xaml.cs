using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.ViewModels;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace Conference.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var savedTheme = Preferences.Get(PreferenceKeys.AppTheme, 0);
        UserAppTheme = savedTheme switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        // Handle notification taps → deep-link to session detail
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var sp = IPlatformApplication.Current?.Services;

        // Load event config synchronously — branding must be applied before pages render
        var configService = sp?.GetService<IEventConfigService>();
        if (configService is Services.EventConfigService ecs)
        {
            ecs.InitializeAsync().GetAwaiter().GetResult();
            ecs.ApplyBranding(Resources);
        }

        // Preload conference data in background (non-blocking)
        _ = Task.Run(async () =>
        {
            try
            {
                var dataService = sp?.GetService<IConferenceDataService>();
                if (dataService != null)
                    await dataService.GetAllDataAsync();

                var reminderService = sp?.GetService<IReminderService>();
                if (reminderService != null)
                    await reminderService.ReconcileRemindersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Non-critical preload error: {ex.Message}");
            }
        });

        if (OnboardingViewModel.IsOnboardingCompleted())
            return new Window(new AppShell());

        var onboardingPage = sp!.GetRequiredService<OnboardingPage>();
        return new Window(onboardingPage);
    }

    private static async void OnNotificationTapped(NotificationActionEventArgs e)
    {
        var sessionId = e.Request?.ReturningData;
        if (string.IsNullOrEmpty(sessionId))
            return;

        // Navigate to session detail — route & param must match Shell registration and QueryProperty
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync($"{nameof(Pages.SessionDetailsPage)}?SessionId={sessionId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Best-effort navigation error: {ex.Message}");
        }
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
