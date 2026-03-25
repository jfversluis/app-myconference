using Conference.Maui.Pages;

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
    }
}
