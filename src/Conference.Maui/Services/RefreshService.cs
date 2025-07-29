using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using Conference.Maui.Models;

namespace Conference.Maui.Services;

public class RefreshService
{
    private readonly DataSyncService _dataSyncService;

    public RefreshService(DataSyncService dataSyncService)
    {
        _dataSyncService = dataSyncService;
    }

    public async Task<RefreshResult> RefreshWithFeedbackAsync(string contentType, bool forceRefresh = false)
    {
        // Check internet connectivity before attempting refresh
        var networkAccess = Connectivity.Current.NetworkAccess;
        if (networkAccess != NetworkAccess.Internet)
        {
            var toast = Toast.Make("No internet connection available", CommunityToolkit.Maui.Core.ToastDuration.Short);
            await toast.Show();
            return RefreshResult.Failed;
        }

        try
        {
            var result = await _dataSyncService.RefreshDataAsync(forceRefresh: forceRefresh);

            // Show appropriate toast based on result
            var toast = result switch
            {
                RefreshResult.DataUpdated => Toast.Make($"{contentType} updated with new data", CommunityToolkit.Maui.Core.ToastDuration.Short),
                RefreshResult.NoUpdateNeeded => Toast.Make($"{contentType} is already up to date", CommunityToolkit.Maui.Core.ToastDuration.Short),
                RefreshResult.Failed => Toast.Make($"Failed to refresh {contentType.ToLower(CultureInfo.InvariantCulture)}", CommunityToolkit.Maui.Core.ToastDuration.Short),
                _ => Toast.Make($"Failed to refresh {contentType.ToLower(CultureInfo.InvariantCulture)}", CommunityToolkit.Maui.Core.ToastDuration.Short)
            };

            await toast.Show();
            return result;
        }
        catch (Exception ex)
        {
            var errorToast = Toast.Make($"Failed to refresh: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
            await errorToast.Show();
            return RefreshResult.Failed;
        }
    }
}
