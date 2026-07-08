using System.Collections;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using Akavache;
using Conference.Maui.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    private readonly IEventConfigService _configService;
    private readonly IEventTimeService _eventTimeService;
    private readonly JsonSerializerSettings _sessionizeJsonSettings;
    private readonly AsyncRetryPolicy _retryPolicy;

    private string AllDataCacheKey => $"sessionize_all_data_v3_timezonefix_rawjson_{_configService.Config.Api.SessionizeEventId}";
    private string ScheduleGridCacheKey => $"sessionize_schedule_grid_v3_timezonefix_rawjson_{_configService.Config.Api.SessionizeEventId}";
    private string LastModifiedCacheKey => $"sessionize_last_modified_{_configService.Config.Api.SessionizeEventId}";
    private string CategoryTagsCacheKey => $"sessionize_category_tags_{_configService.Config.Api.SessionizeEventId}";
    private string TagSessionCountsCacheKey => $"sessionize_tag_session_counts_{_configService.Config.Api.SessionizeEventId}";
    private string SessionTagMapCacheKey => $"sessionize_session_tag_map_{_configService.Config.Api.SessionizeEventId}";

    public ConferenceDataService(
        ISessionizeApiClient sessionizeClient,
        IHttpClientFactory httpClientFactory,
        IEventConfigService configService,
        IEventTimeService eventTimeService,
        ILogger<ConferenceDataService> logger)
    {
        _sessionizeClient = sessionizeClient;
        _configService = configService;
        _eventTimeService = eventTimeService;
        _sessionizeClient.SessionizeApiId = _configService.Config.Api.SessionizeEventId;
        _httpClientFactory = httpClientFactory;
        _cache = BlobCache.LocalMachine;
        _logger = logger;
        _sessionizeJsonSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Converters = [new SessionizeLocalDateTimeOffsetConverter(_eventTimeService)]
        };

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                _configService.Config.Api.MaxRetryAttempts,
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
                    cachedData = await GetCachedSessionizeResponseAsync<AllDataResponse>(AllDataCacheKey);
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
                        if (!await HasDataChangedAsync(CancellationToken.None))
                        {
                            _logger.LogDebug("Data unchanged on server, skipping background refresh");
                            return;
                        }

                        var freshData = await FetchFromApiAsync(CancellationToken.None);
                        if (freshData.Data != null)
                        {
                            await CacheSessionizeResponseAsync(
                                AllDataCacheKey,
                                freshData.Json,
                                TimeSpan.FromHours(_configService.Config.Api.CacheExpirationHours));
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

                if (freshData.Data != null)
                {
                    await CacheSessionizeResponseAsync(
                        AllDataCacheKey,
                        freshData.Json,
                        TimeSpan.FromHours(_configService.Config.Api.CacheExpirationHours));

                    _logger.LogInformation("Conference data fetched and cached");
                    return freshData.Data;
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

    private async Task<(AllDataResponse? Data, string Json)> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        var payload = await _retryPolicy.ExecuteAsync(ct => FetchSessionizePayloadAsync<AllDataResponse>("view/All", ct), cancellationToken);
        var data = NormalizeSessionizeTimes(payload.Data);

        if (data != null)
        {
            // Store current server timestamp so HasDataChangedAsync can compare later
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                var request = new HttpRequestMessage(HttpMethod.Head,
                    $"{_configService.Config.Api.SessionizeBaseUrl}{_configService.Config.Api.SessionizeEventId}/view/All");
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

        return (data, payload.Json);
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
                    var cachedData = await GetCachedSessionizeResponseAsync<List<ScheduleGridResponse>>(ScheduleGridCacheKey);
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
            var freshData = await _retryPolicy.ExecuteAsync(ct => FetchSessionizePayloadAsync<List<ScheduleGridResponse>>("view/grid-smart", ct), cancellationToken);

            var normalizedData = NormalizeSessionizeTimes(freshData.Data);

            if (normalizedData != null)
            {
                await CacheSessionizeResponseAsync(
                    ScheduleGridCacheKey,
                    freshData.Json,
                    TimeSpan.FromHours(_configService.Config.Api.CacheExpirationHours));

                _logger.LogInformation("Schedule grid refreshed and cached");
            }

            return normalizedData ?? [];
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
            
            var freshData = await _retryPolicy.ExecuteAsync(ct => FetchSessionizePayloadAsync<List<ScheduleGridResponse>>("view/grid-smart", ct), cancellationToken);

            var normalizedData = NormalizeSessionizeTimes(freshData.Data);

            if (normalizedData != null)
            {
                await CacheSessionizeResponseAsync(
                    ScheduleGridCacheKey,
                    freshData.Json,
                    TimeSpan.FromHours(_configService.Config.Api.CacheExpirationHours));

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
                $"{_configService.Config.Api.SessionizeBaseUrl}{_configService.Config.Api.SessionizeEventId}/view/All");
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
        cts.CancelAfter(TimeSpan.FromSeconds(_configService.Config.Api.ApiTimeoutSeconds));

        var url = $"{_configService.Config.Api.SessionizeBaseUrl}{_configService.Config.Api.SessionizeEventId}/view/All";
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
            var expiry = TimeSpan.FromHours(_configService.Config.Api.CacheExpirationHours);
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

    private T? NormalizeSessionizeTimes<T>(T? value) where T : class
    {
        if (value is null)
            return null;

        NormalizeObjectGraph(value, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return value;
    }

    private void NormalizeObjectGraph(object value, HashSet<object> visited)
    {
        var type = value.GetType();
        if (IsTerminal(type))
            return;

        if (!type.IsValueType && !visited.Add(value))
            return;

        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                    NormalizeObjectGraph(item, visited);
            }

            return;
        }

        if (!IsSessionizeType(type))
            return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            if (property.PropertyType == typeof(DateTimeOffset) &&
                property.CanWrite &&
                (property.Name == "StartsAt" || property.Name == "EndsAt"))
            {
                var timestamp = (DateTimeOffset?)property.GetValue(value);
                if (timestamp.HasValue)
                    property.SetValue(value, _eventTimeService.NormalizeSessionizeLocalTime(timestamp.Value));

                continue;
            }

            var propertyValue = property.GetValue(value);
            if (propertyValue is not null)
                NormalizeObjectGraph(propertyValue, visited);
        }
    }

    private static bool IsSessionizeType(Type type) =>
        type.Namespace?.StartsWith("Sessionize.Api.Client", StringComparison.Ordinal) == true;

    private static bool IsTerminal(Type type) =>
        type.IsPrimitive ||
        type.IsEnum ||
        type == typeof(string) ||
        type == typeof(decimal) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(DateOnly) ||
        type == typeof(TimeOnly) ||
        type == typeof(Guid);

    private async Task<T?> GetCachedSessionizeResponseAsync<T>(string cacheKey)
    {
        var cachedJson = await _cache.GetObject<string>(cacheKey);
        return DeserializeSessionize<T>(cachedJson);
    }

    private Task CacheSessionizeResponseAsync(string cacheKey, string json, TimeSpan expiry) =>
        _cache.InsertObject(cacheKey, json, expiry).ToTask();

    private async Task<(T? Data, string Json)> FetchSessionizePayloadAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_configService.Config.Api.ApiTimeoutSeconds));

        var client = _httpClientFactory.CreateClient();
        var url = $"{_configService.Config.Api.SessionizeBaseUrl}{_configService.Config.Api.SessionizeEventId}/{relativePath}";
        var json = await client.GetStringAsync(url, cts.Token);

        return (DeserializeSessionize<T>(json), json);
    }

    private T? DeserializeSessionize<T>(string json)
    {
        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader)
        {
            DateParseHandling = DateParseHandling.None
        };

        return Newtonsoft.Json.JsonSerializer.Create(_sessionizeJsonSettings).Deserialize<T>(jsonReader);
    }
}
