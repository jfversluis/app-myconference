using Akavache;
using CommunityToolkit.Maui;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Conference.Maui.Views.About;
using Conference.Maui.Views.Favorites;
using Conference.Maui.Views.Sessions;
using Conference.Maui.Views.Settings;
using Conference.Maui.Views.Speakers;
using Microsoft.Extensions.Logging;

namespace Conference.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Initialize Akavache
		BlobCache.ApplicationName = "MyConference";

		// Register HttpClient
		builder.Services.AddSingleton<HttpClient>();

		// Register Services
		builder.Services.AddSingleton<ISessionizeService, SessionizeService>();
		builder.Services.AddSingleton<IFavoritesService, FavoritesService>();

		// Register ViewModels
		builder.Services.AddTransient<SessionsViewModel>();
		builder.Services.AddTransient<SpeakersViewModel>();
		builder.Services.AddTransient<FavoritesViewModel>();
		builder.Services.AddTransient<AboutViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<SessionDetailViewModel>();
		builder.Services.AddTransient<SpeakerDetailViewModel>();

		// Register Views
		builder.Services.AddTransient<SessionsPage>();
		builder.Services.AddTransient<SpeakersPage>();
		builder.Services.AddTransient<FavoritesPage>();
		builder.Services.AddTransient<AboutPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SessionDetailPage>();
		builder.Services.AddTransient<SpeakerDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
