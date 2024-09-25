using Conference.Maui.Pages;

namespace Conference.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("SessionDetails", typeof(SessionDetailsPage));
        Routing.RegisterRoute("SpeakerDetails", typeof(SpeakerDetailsPage));
    }
}
