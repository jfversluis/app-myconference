using Conference.Maui.Pages;
using Conference.Maui.ViewModels;
using Microsoft.Extensions.Logging;

namespace Conference.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SessionDetailsPage), typeof(SessionDetailsPage));
        Routing.RegisterRoute(nameof(SpeakerDetailsPage), typeof(SpeakerDetailsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(QuickPickPage), typeof(QuickPickPage));
        Routing.RegisterRoute(nameof(ConflictResolverPage), typeof(ConflictResolverPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!OnboardingViewModel.IsOnboardingCompleted())
        {
            await Task.Delay(500); // Shell needs time to be fully ready
            try
            {
                var sp = IPlatformApplication.Current?.Services;
                if (sp != null)
                {
                    var onboardingPage = sp.GetRequiredService<OnboardingPage>();
                    await Shell.Current.Navigation.PushModalAsync(onboardingPage, animated: false);
                }
            }
            catch (Exception ex)
            {
                var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<AppShell>>();
                logger?.LogError(ex, "Failed to show onboarding");
            }
        }
    }
}
