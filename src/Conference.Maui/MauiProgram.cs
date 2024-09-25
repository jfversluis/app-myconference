using CommunityToolkit.Maui;
using Conference.Maui.Interfaces;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using Conference.Maui.ViewModels;
using Microsoft.Extensions.Logging;

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
            })
            .UseMauiCommunityToolkit();

        // TODO I don't think HttpClientFactory is great for mobile...?
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IEventDataService, SessionizeService>();
        
        builder.Services.AddTransient<ScheduleViewModel>();
        builder.Services.AddTransient<SchedulePage>();

        builder.Services.AddTransient<SpeakersViewModel>();
        builder.Services.AddTransient<SpeakersPage>();

        builder.Services.AddTransient<SessionDetailsViewModel>();
        builder.Services.AddTransient<SessionDetailsPage>();

        builder.Services.AddTransient<SpeakerDetailsViewModel>();
        builder.Services.AddTransient<SpeakerDetailsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
