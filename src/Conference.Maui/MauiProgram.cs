using Akavache;
using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.V10toV11;
using CommunityToolkit.Maui;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Sessionize.Api.Client;
using Sessionize.Api.Client.Abstractions;
using Sessionize.Api.Client.Configuration;
using Splat.Builder;
using Syncfusion.Maui.Toolkit.Hosting;
using MauiIcons.Fluent;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace Conference.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(cache =>
                cache.WithSqliteProvider()
                    .WithSqliteDefaults()
                    .WithV10FileNames(),
                "Conference.Maui");

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
                handlers.AddHandler<CollectionView, Conference.Maui.Platforms.iOS.StickyHeaderCollectionViewHandler>();
            })
#endif
            .UseMauiCommunityToolkit()
            .UseFluentMauiIcons()
            .UseLocalNotification()
            .ConfigureSyncfusionToolkit();

        // Remove native border from Entry on iOS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>
        {
#if IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });

        // Register config service (must be before Sessionize which depends on it)
        builder.Services.AddSingleton<IEventConfigService, EventConfigService>();
        builder.Services.AddSingleton<IEventTimeService, EventTimeService>();

        // Configure Sessionize API client (defaults overridden at startup after config loads)
        builder.Services.Configure<SessionizeConfiguration>(options =>
        {
            options.BaseUrl = AppConfig.SessionizeBaseUrl;
            options.ApiId = AppConfig.SessionizeApiId;
        });
        builder.Services.AddHttpClient<SessionizeApiClient>();
        builder.Services.AddSingleton<ISessionizeApiClient, SessionizeApiClient>();

        // Register services
        builder.Services.AddSingleton<IConferenceDataService, ConferenceDataService>();
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
        builder.Services.AddSingleton<IHapticService, HapticService>();
        builder.Services.AddSingleton<IReminderService, ReminderService>();
        builder.Services.AddSingleton<ISessionItemMapper, SessionItemMapper>();

        // Register view models
        builder.Services.AddTransient<MyEventViewModel>();
        builder.Services.AddTransient<SessionsViewModel>();
        builder.Services.AddTransient<SpeakersViewModel>();
        builder.Services.AddTransient<SessionDetailsViewModel>();
        builder.Services.AddTransient<SpeakerDetailsViewModel>();
        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<QuickPickViewModel>();
        builder.Services.AddTransient<ConflictResolverViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();

        // Register pages
        builder.Services.AddTransient<MyEventPage>();
        builder.Services.AddTransient<SessionsPage>();
        builder.Services.AddTransient<SpeakersPage>();
        builder.Services.AddTransient<SessionDetailsPage>();
        builder.Services.AddTransient<SpeakerDetailsPage>();
        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QuickPickPage>();
        builder.Services.AddTransient<ConflictResolverPage>();
        builder.Services.AddTransient<OnboardingPage>();

#if DEBUG
        builder.Logging.AddDebug();
        builder.AddMauiDevFlowAgent();
#endif

        return builder.Build();
    }
}
