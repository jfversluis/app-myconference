using Conference.Maui.Models;

namespace Conference.Maui.Interfaces;

public interface ISponsorService
{
    Task<List<Sponsor>> GetAllSponsors();
}
