using CommunityToolkit.Maui;
using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using Plugin.Maui.SwipeCardView;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Conference.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemibold");
            })
#if IOS
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
            })
#endif
            .UseMauiCommunityToolkit()
            .UseSwipeCardView() // Add SwipeCardView initialization
            .ConfigureSyncfusionToolkit();

        // Register services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>(); // Add DatabaseService first
        builder.Services.AddSingleton<IEventDataService, SessionizeService>();
        builder.Services.AddSingleton<ISponsorService, SponsorService>();
        builder.Services.AddSingleton<DataSyncService>(); // Add DataSyncService
        builder.Services.AddSingleton<RefreshService>(); // Add RefreshService

        // Register view models and pages
        builder.Services.AddTransient<ScheduleViewModel>();
        builder.Services.AddTransient<SchedulePage>();

        builder.Services.AddTransient<SpeakersViewModel>();
        builder.Services.AddTransient<SpeakersPage>();

        builder.Services.AddTransient<SessionDetailsViewModel>();
        builder.Services.AddTransient<SessionDetailsPage>();

        builder.Services.AddTransient<SpeakerDetailsViewModel>();
        builder.Services.AddTransient<SpeakerDetailsPage>();

        builder.Services.AddTransient<SponsorsViewModel>();
        builder.Services.AddTransient<SponsorsPage>();
        
        // Add MyAgenda view model and page
        builder.Services.AddTransient<MyAgendaViewModel>();
        builder.Services.AddTransient<MyAgendaPage>();

        builder.Services.AddTransient<PickFavoriteSessionsViewModel>();
        builder.Services.AddTransient<PickFavoriteSessionsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
