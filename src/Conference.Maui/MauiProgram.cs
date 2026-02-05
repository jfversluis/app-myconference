using Akavache;
using CommunityToolkit.Maui;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client;
using Sessionize.Api.Client.Abstractions;
using Sessionize.Api.Client.Configuration;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Conference.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Initialize Akavache
        Registrations.Start("Conference.Maui");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
#if IOS
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
            })
#endif
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionToolkit();

        // Configure Sessionize API client
        builder.Services.Configure<SessionizeConfiguration>(options =>
        {
            options.BaseUrl = AppConfig.SessionizeBaseUrl;
            options.ApiId = AppConfig.SessionizeApiId;
        });
        builder.Services.AddHttpClient<SessionizeApiClient>();
        builder.Services.AddScoped<ISessionizeApiClient, SessionizeApiClient>();

        // Register services
        builder.Services.AddSingleton<IConferenceDataService, ConferenceDataService>();
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();

        // Register view models
        builder.Services.AddTransient<SessionsViewModel>();
        builder.Services.AddTransient<SpeakersViewModel>();
        builder.Services.AddTransient<SessionDetailsViewModel>();
        builder.Services.AddTransient<SpeakerDetailsViewModel>();
        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<AboutViewModel>();

        // Register pages
        builder.Services.AddTransient<SessionsPage>();
        builder.Services.AddTransient<SpeakersPage>();
        builder.Services.AddTransient<SessionDetailsPage>();
        builder.Services.AddTransient<SpeakerDetailsPage>();
        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
