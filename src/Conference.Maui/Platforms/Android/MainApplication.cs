using Akavache;
using Android.App;
using Android.Runtime;

namespace Conference.Maui;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp()
	{
		// Initialize Akavache for Android before anything else
		Akavache.Registrations.Start("MyConference");
		
		return MauiProgram.CreateMauiApp();
	}
}
