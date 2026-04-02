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
            ecs.Initialize();
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
                if (reminderService != null && (configService?.Config.Features.EnableReminders ?? true))
                    await reminderService.ReconcileRemindersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Non-critical preload error: {ex.Message}");
            }
        });

        // Skip onboarding if feature is disabled or already completed
        var isOnboardingEnabled = configService?.Config.Features.EnableOnboarding ?? true;
        if (!isOnboardingEnabled || OnboardingViewModel.IsOnboardingCompleted())
            return new Window(new AppShell());

        var onboardingPage = sp?.GetService<OnboardingPage>();
        if (onboardingPage is null)
            return new Window(new AppShell());

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
            var shell = new AppShell();
            window.Page = shell;

            try
            {
                // Wait for Shell to be fully ready (up to 3s) instead of a fixed delay
                for (int i = 0; i < 30 && Shell.Current is null; i++)
                    await Task.Delay(100);

                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Post-onboarding navigation error: {ex.Message}");
            }
        }
    }
}
