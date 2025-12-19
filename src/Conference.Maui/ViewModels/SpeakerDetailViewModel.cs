using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Services;

namespace Conference.Maui.ViewModels;

public partial class SpeakerDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ISessionizeService _sessionizeService;
    private string _source = string.Empty;

    [ObservableProperty]
    private Speaker? speaker;

    public SpeakerDetailViewModel(ISessionizeService sessionizeService)
    {
        _sessionizeService = sessionizeService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("Speaker"))
        {
            Speaker = query["Speaker"] as Speaker;
            Title = Speaker?.FullName ?? "Speaker Details";
        }

        if (query.ContainsKey("Source"))
        {
            _source = query["Source"] as string ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task SessionTappedAsync(SessionLink sessionLink)
    {
        if (sessionLink == null)
            return;

        if (_source == "SessionDetail")
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        var allSessions = await _sessionizeService.GetSessionsAsync();
        var session = allSessions.FirstOrDefault(s => s.Id == sessionLink.Id.ToString());

        if (session != null)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Session", session },
                { "Source", "SpeakerDetail" }
            };

            await Shell.Current.GoToAsync($"sessiondetail", navigationParameter);
        }
    }

    [RelayCommand]
    private async Task OpenUrlAsync(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            await Launcher.OpenAsync(url);
        }
    }
}
