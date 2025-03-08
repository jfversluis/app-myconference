using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Conference.Maui.ViewModels;

public partial class SponsorsViewModel : ObservableObject
{
    private readonly ISponsorService _sponsorService;

    public ObservableCollection<Sponsor> Sponsors { get; set; } = new ObservableCollection<Sponsor>();

    public SponsorsViewModel(ISponsorService sponsorService)
    {
        _sponsorService = sponsorService;
    }

    public async Task LoadSponsorsData()
    {
        var sponsors = await _sponsorService.GetAllSponsors();

        Sponsors.Clear();
        foreach (var sponsor in sponsors)
        {
            Sponsors.Add(sponsor);
        }
    }

    [RelayCommand]
    private async Task OpenSponsorWebsite(Sponsor sponsor)
    {
        if (!string.IsNullOrEmpty(sponsor.Website))
        {
            try
            {
                Uri uri = new Uri(sponsor.Website);
                await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                // Handle navigation failure
            }
        }
    }
}
