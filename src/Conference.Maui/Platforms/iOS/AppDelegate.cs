using Akavache;
using Foundation;

namespace Conference.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp()
	{
		// Initialize Akavache for iOS before anything else
		Akavache.Registrations.Start("MyConference");
		
		return MauiProgram.CreateMauiApp();
	}
}
