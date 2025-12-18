using Conference.Maui.Views.Sessions;
using Conference.Maui.Views.Speakers;
using Conference.Maui.Views.Settings;

namespace Conference.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("sessiondetail", typeof(SessionDetailPage));
		Routing.RegisterRoute("speakerdetail", typeof(SpeakerDetailPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("licenses", typeof(LicensesPage));
	}
}
