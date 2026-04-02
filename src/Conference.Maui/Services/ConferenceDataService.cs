using System.Reactive.Linq;
using System.Text.Json;
using Akavache;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Sessionize.Api.Client.Abstractions;
using Sessionize.Api.Client.DataTransferObjects;

namespace Conference.Maui.Services;

public class ConferenceDataService : IConferenceDataService
{
    private readonly ISessionizeApiClient _sessionizeClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBlobCache _cache;
    private readonly ILogger<ConferenceDataService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    private const string AllDataCacheKey = "sessionize_all_data";
    private const string ScheduleGridCacheKey = "sessionize_schedule_grid";
    private const string LastModifiedCacheKey = "sessionize_last_modified";
    private const string CategoryTagsCacheKey = "sessionize_category_tags";
    private const string TagSessionCountsCacheKey = "sessionize_tag_session_counts";
    private const string SessionTagMapCacheKey = "sessionize_session_tag_map";

    public ConferenceDataService(
        ISessionizeApiClient sessionizeClient,
        IHttpClientFactory httpClientFactory,
        ILogger<ConferenceDataService> logger)
    {
        _sessionizeClient = sessionizeClient;
        _sessionizeClient.SessionizeApiId = AppConfig.SessionizeApiId;
        _httpClientFactory = httpClientFactory;
        _cache = BlobCache.LocalMachine;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                AppConfig.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} after {Delay}s due to: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
    }

    public async Task<AllDataResponse?> GetAllDataAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get cached data first (for immediate display)
            AllDataResponse? cachedData = null;
            if (!forceRefresh)
            {
                try
                {
                    cachedData = await _cache.GetObject<AllDataResponse>(AllDataCacheKey);
                    _logger.LogDebug("Loaded conference data from cache");
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogDebug("No cached conference data found");
                }
            }

            // If we have cached data, return it immediately and refresh in background only if needed
            if (cachedData != null && !forceRefresh)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Quick HEAD check: has data changed on the server?
                        if (!await HasDataChangedAsync(cancellationToken))
                        {
                            _logger.LogDebug("Data unchanged on server, skipping background refresh");
                            return;
                        }

                        var freshData = await FetchFromApiAsync(cancellationToken);
                        if (freshData != null)
                        {
                            await _cache.InsertObject(
                                AllDataCacheKey,
                                freshData,
                                TimeSpan.FromHours(AppConfig.CacheExpirationHours));
                            _logger.LogInformation("Conference data refreshed in background");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background refresh failed");
                    }
                });
                return cachedData;
            }

            // No cache — must fetch from API (blocks)
            try
            {
                var freshData = await FetchFromApiAsync(cancellationToken);

                if (freshData != null)
                {
                    await _cache.InsertObject(
                        AllDataCacheKey,
                        freshData,
                        TimeSpan.FromHours(AppConfig.CacheExpirationHours));

                    _logger.LogInformation("Conference data fetched and cached");
                    return freshData;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to fetch data from API");
            }

            return cachedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conference data");
            return null;
        }
    }

    private async Task<AllDataResponse?> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var data = await _retryPolicy.ExecuteAsync(async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.ApiTimeoutSeconds));
            return await _sessionizeClient.GetAllDataAsync(cancellationToken: cts.Token);
        });

        if (data != null)
        {
            // Store current server timestamp so HasDataChangedAsync can compare later
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                var request = new HttpRequestMessage(HttpMethod.Head,
                    $"{AppConfig.SessionizeBaseUrl}{AppConfig.SessionizeApiId}/view/All");
                var response = await client.SendAsync(request, cts.Token);
                if (response.Content.Headers.LastModified.HasValue)
                {
                    await _cache.InsertObject(LastModifiedCacheKey,
                        response.Content.Headers.LastModified.Value.UtcTicks,
                        TimeSpan.FromDays(30));
                }
            }
            catch
            {
                // Non-critical — just means next check will re-download
            }
        }

        return data;
    }

    public async Task<List<ScheduleGridResponse>> GetScheduleGridAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get cached data first
            if (!forceRefresh)
            {
                try
                {
                    var cachedData = await _cache.GetObject<List<ScheduleGridResponse>>(ScheduleGridCacheKey);
                    if (cachedData != null && cachedData.Count > 0)
                    {
                        _logger.LogDebug("Loaded schedule grid from cache");
                        
                        // Refresh in background
                        _ = RefreshScheduleGridInBackgroundAsync(cancellationToken);
                        
                        return cachedData;
                    }
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogDebug("No cached schedule grid found");
                }
            }

            // Fetch from API
            var freshData = await _retryPolicy.ExecuteAsync(async () =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.ApiTimeoutSeconds));
                return await _sessionizeClient.GetScheduleGridAsync(cancellationToken: cts.Token);
            });

            if (freshData != null)
            {
                await _cache.InsertObject(
                    ScheduleGridCacheKey,
                    freshData,
                    TimeSpan.FromHours(AppConfig.CacheExpirationHours));

                _logger.LogInformation("Schedule grid refreshed and cached");
            }

            return freshData ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule grid");
            return [];
        }
    }

    private async Task RefreshScheduleGridInBackgroundAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken); // Small delay to not block UI
            
            var freshData = await _retryPolicy.ExecuteAsync(async () =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.ApiTimeoutSeconds));
                return await _sessionizeClient.GetScheduleGridAsync(cancellationToken: cts.Token);
            });

            if (freshData != null)
            {
                await _cache.InsertObject(
                    ScheduleGridCacheKey,
                    freshData,
                    TimeSpan.FromHours(AppConfig.CacheExpirationHours));

                _logger.LogDebug("Schedule grid background refresh completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background refresh of schedule grid failed");
        }
    }

    public async Task<bool> HasDataChangedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get stored Last-Modified timestamp
            long storedTicks;
            try
            {
                storedTicks = await _cache.GetObject<long>(LastModifiedCacheKey);
            }
            catch (KeyNotFoundException)
            {
                return true; // No stored timestamp = treat as changed
            }

            // Quick HEAD request to get current Last-Modified (~50ms, no body)
            var client = _httpClientFactory.CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var request = new HttpRequestMessage(HttpMethod.Head,
                $"{AppConfig.SessionizeBaseUrl}{AppConfig.SessionizeApiId}/view/All");
            var response = await client.SendAsync(request, cts.Token);

            if (!response.Content.Headers.LastModified.HasValue)
                return true; // Can't determine, assume changed

            var serverTicks = response.Content.Headers.LastModified.Value.UtcTicks;
            var hasChanged = serverTicks != storedTicks;

            _logger.LogDebug("Data change check: stored={StoredTicks}, server={ServerTicks}, changed={Changed}",
                storedTicks, serverTicks, hasChanged);

            return hasChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if data changed, assuming changed");
            return true;
        }
    }

    public async Task<Dictionary<int, string>> GetCategoryTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            try
            {
                var cached = await _cache.GetObject<Dictionary<int, string>>(CategoryTagsCacheKey);
                if (cached is { Count: > 0 })
                {
                    _logger.LogDebug("Loaded {Count} category tags from cache", cached.Count);
                    return cached;
                }
            }
            catch (KeyNotFoundException) { }

            // Fetch from API and compute both tags + counts
            await FetchAndCacheTagDataAsync(cancellationToken);

            try
            {
                var result = await _cache.GetObject<Dictionary<int, string>>(CategoryTagsCacheKey);
                return result ?? [];
            }
            catch (KeyNotFoundException)
            {
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching category tags");
            return [];
        }
    }

    public async Task<Dictionary<int, int>> GetTagSessionCountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            try
            {
                var cached = await _cache.GetObject<Dictionary<int, int>>(TagSessionCountsCacheKey);
                if (cached is { Count: > 0 })
                    return cached;
            }
            catch (KeyNotFoundException) { }

            // Fetch from API
            await FetchAndCacheTagDataAsync(cancellationToken);

            try
            {
                var result = await _cache.GetObject<Dictionary<int, int>>(TagSessionCountsCacheKey);
                return result ?? [];
            }
            catch (KeyNotFoundException)
            {
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching tag session counts");
            return [];
        }
    }

    public async Task<Dictionary<string, List<int>>> GetSessionTagMapAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            try
            {
                var cached = await _cache.GetObject<Dictionary<string, List<int>>>(SessionTagMapCacheKey);
                if (cached is { Count: > 0 })
                    return cached;
            }
            catch (KeyNotFoundException) { }

            await FetchAndCacheTagDataAsync(cancellationToken);

            try
            {
                var result = await _cache.GetObject<Dictionary<string, List<int>>>(SessionTagMapCacheKey);
                return result ?? [];
            }
            catch (KeyNotFoundException)
            {
                return [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching session tag map");
            return [];
        }
    }

    private async Task FetchAndCacheTagDataAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.ApiTimeoutSeconds));

        var url = $"{AppConfig.SessionizeBaseUrl}{AppConfig.SessionizeApiId}/view/All";
        var json = await client.GetStringAsync(url, cts.Token);
        using var doc = JsonDocument.Parse(json);

        // Extract tag names from categories
        var tagMap = new Dictionary<int, string>();
        var tagIds = new HashSet<int>();
        if (doc.RootElement.TryGetProperty("categories", out var categories))
        {
            foreach (var cat in categories.EnumerateArray())
            {
                var title = cat.GetProperty("title").GetString() ?? "";
                if (!title.Contains("tag", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("other", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var item in cat.GetProperty("items").EnumerateArray())
                {
                    var id = item.GetProperty("id").GetInt32();
                    var name = item.GetProperty("name").GetString() ?? "";
                    tagMap[id] = name;
                    tagIds.Add(id);
                }
                break;
            }
        }

        // Count sessions per tag and build session→tag mapping from the sessions array
        var tagCounts = new Dictionary<int, int>();
        var sessionTagMap = new Dictionary<string, List<int>>();
        if (doc.RootElement.TryGetProperty("sessions", out var sessions))
        {
            foreach (var session in sessions.EnumerateArray())
            {
                if (session.TryGetProperty("isServiceSession", out var isSvc) && isSvc.GetBoolean())
                    continue;
                if (!session.TryGetProperty("categoryItems", out var catItems))
                    continue;

                var sessionId = session.GetProperty("id").GetString() ?? "";
                var sessionTags = new List<int>();
                foreach (var catId in catItems.EnumerateArray())
                {
                    var id = catId.GetInt32();
                    if (tagIds.Contains(id))
                    {
                        tagCounts[id] = tagCounts.GetValueOrDefault(id) + 1;
                        sessionTags.Add(id);
                    }
                }
                if (sessionTags.Count > 0)
                    sessionTagMap[sessionId] = sessionTags;
            }
        }

        if (tagMap.Count > 0)
        {
            var expiry = TimeSpan.FromHours(AppConfig.CacheExpirationHours);
            await _cache.InsertObject(CategoryTagsCacheKey, tagMap, expiry);
            await _cache.InsertObject(TagSessionCountsCacheKey, tagCounts, expiry);
            await _cache.InsertObject(SessionTagMapCacheKey, sessionTagMap, expiry);
            _logger.LogInformation("Cached {Tags} tags, {Counts} tag counts, {Map} session-tag mappings",
                tagMap.Count, tagCounts.Count, sessionTagMap.Count);
        }
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            await _cache.InvalidateObject<AllDataResponse>(AllDataCacheKey);
            await _cache.InvalidateObject<List<ScheduleGridResponse>>(ScheduleGridCacheKey);
            await _cache.InvalidateObject<long>(LastModifiedCacheKey);
            await _cache.InvalidateObject<Dictionary<int, string>>(CategoryTagsCacheKey);
            await _cache.InvalidateObject<Dictionary<int, int>>(TagSessionCountsCacheKey);
            await _cache.InvalidateObject<Dictionary<string, List<int>>>(SessionTagMapCacheKey);
            _logger.LogInformation("Cache cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }
}
