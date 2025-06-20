using Conference.Maui.Interfaces;
using Conference.Maui.Models;

namespace Conference.Maui.Extensions;

public static class SessionExtensions
{
    public static async Task<bool> IsFavoriteAsync(this Session session, IDatabaseService databaseService)
    {
        return await databaseService.IsFavoriteSessionAsync(session.Id);
    }

    public static async Task<Session> WithFavoriteStatusAsync(this Session session, IDatabaseService databaseService)
    {
        // We don't modify the original session object, but this method can be used
        // to check favorite status when needed
        return session;
    }
}