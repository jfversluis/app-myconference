using Conference.Maui.Views.About;
using Conference.Maui.Views.Favorites;
using Conference.Maui.Views.Sessions;
using Conference.Maui.Views.Speakers;
using Conference.Maui.Views.Settings;

namespace Conference.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register main tab routes
		Routing.RegisterRoute("sessions", typeof(SessionsPage));
		Routing.RegisterRoute("speakers", typeof(SpeakersPage));
		Routing.RegisterRoute("favorites", typeof(FavoritesPage));
		Routing.RegisterRoute("about", typeof(AboutPage));
		
		// Register detail routes for navigation
		Routing.RegisterRoute("sessiondetail", typeof(SessionDetailPage));
		Routing.RegisterRoute("speakerdetail", typeof(SpeakerDetailPage));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("licenses", typeof(LicensesPage));
	}
}
