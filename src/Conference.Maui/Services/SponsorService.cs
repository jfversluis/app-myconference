using System.Text.Json;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class SponsorService : ISponsorService
{
    private List<Sponsor> _sponsors = [];

    public async Task<List<Sponsor>> GetAllSponsors()
    {
        if (_sponsors.Count > 0)
            return _sponsors;

        using var stream = await FileSystem.OpenAppPackageFileAsync("sponsors.json");
        using var reader = new StreamReader(stream);
        
        var json = await reader.ReadToEndAsync();
        _sponsors = JsonSerializer.Deserialize<List<Sponsor>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        return _sponsors;
    }
}
