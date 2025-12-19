using Akavache;
using Conference.Maui.Models;
using Polly;
using Polly.Retry;
using System.Reactive.Linq;
using System.Text.Json;

namespace Conference.Maui.Services;

public class SessionizeService : ISessionizeService
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private const string BaseUrl = "https://sessionize.com/api/v2";
    private const string CacheKeyPrefix = "sessionize_";
    private const string HashKey = "data_hash";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public SessionizeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                });
    }

    public async Task<string> GetDataHashAsync()
    {
        try
        {
            var url = $"{BaseUrl}/{AppConfig.SessionizeId}/view/All?hashOnly=true";
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetStringAsync(url));
            return response.Trim('"');
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching hash: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<List<Session>> GetSessionsAsync(bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}sessions";
        
        if (!forceRefresh)
        {
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<Session>>(cacheKey);
                if (cached?.Any() == true)
                {
                    return cached;
                }
            }
            catch { }
        }

        try
        {
            var schedule = await GetScheduleAsync(forceRefresh);
            var sessions = schedule
                .SelectMany(d => d.TimeSlots)
                .SelectMany(ts => ts) // TimeSlot is now the collection
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            await BlobCache.LocalMachine.InsertObject(cacheKey, sessions, DateTimeOffset.Now.Add(CacheDuration));
            return sessions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching sessions: {ex.Message}");
            
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<Session>>(cacheKey);
                return cached ?? new List<Session>();
            }
            catch
            {
                return new List<Session>();
            }
        }
    }

    public async Task<List<Speaker>> GetSpeakersAsync(bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}speakers";
        
        if (!forceRefresh)
        {
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<Speaker>>(cacheKey);
                if (cached?.Any() == true)
                {
                    return cached;
                }
            }
            catch { }
        }

        try
        {
            var url = $"{BaseUrl}/{AppConfig.SessionizeId}/view/Speakers";
            var json = await _retryPolicy.ExecuteAsync(() => _httpClient.GetStringAsync(url));
            var speakers = JsonSerializer.Deserialize<List<Speaker>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Speaker>();

            await BlobCache.LocalMachine.InsertObject(cacheKey, speakers, DateTimeOffset.Now.Add(CacheDuration));
            return speakers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching speakers: {ex.Message}");
            
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<Speaker>>(cacheKey);
                return cached ?? new List<Speaker>();
            }
            catch
            {
                return new List<Speaker>();
            }
        }
    }

    public async Task<List<DaySchedule>> GetScheduleAsync(bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}schedule";
        
        if (!forceRefresh)
        {
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<DaySchedule>>(cacheKey);
                if (cached?.Any() == true)
                {
                    var currentHash = await GetCachedHashAsync();
                    var newHash = await GetDataHashAsync();
                    
                    if (!string.IsNullOrEmpty(currentHash) && 
                        !string.IsNullOrEmpty(newHash) && 
                        currentHash == newHash)
                    {
                        return cached;
                    }
                }
            }
            catch { }
        }

        try
        {
            // Load speakers first to get profile pictures
            var speakers = await GetSpeakersAsync(forceRefresh);
            var speakerLookup = speakers.ToDictionary(s => s.Id, s => s);
            
            var url = $"{BaseUrl}/{AppConfig.SessionizeId}/view/GridSmart";
            var json = await _retryPolicy.ExecuteAsync(() => _httpClient.GetStringAsync(url));
            
            var scheduleData = JsonSerializer.Deserialize<List<GridSmartDay>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<GridSmartDay>();

            var schedule = ConvertToSchedule(scheduleData, speakerLookup);
            
            await BlobCache.LocalMachine.InsertObject(cacheKey, schedule, DateTimeOffset.Now.Add(CacheDuration));
            
            var hash = await GetDataHashAsync();
            if (!string.IsNullOrEmpty(hash))
            {
                await BlobCache.LocalMachine.InsertObject(HashKey, hash);
            }
            
            return schedule;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching schedule: {ex.Message}");
            
            try
            {
                var cached = await BlobCache.LocalMachine.GetObject<List<DaySchedule>>(cacheKey);
                return cached ?? new List<DaySchedule>();
            }
            catch
            {
                return new List<DaySchedule>();
            }
        }
    }

    private async Task<string> GetCachedHashAsync()
    {
        try
        {
            return await BlobCache.LocalMachine.GetObject<string>(HashKey);
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<DaySchedule> ConvertToSchedule(List<GridSmartDay> gridData, Dictionary<string, Speaker> speakerLookup)
    {
        var result = new List<DaySchedule>();

        foreach (var day in gridData)
        {
            var daySchedule = new DaySchedule
            {
                Date = DateTime.Parse(day.Date),
                DateString = day.Date,
                TimeSlots = new List<TimeSlot>()
            };

            foreach (var slot in day.TimeSlots)
            {
                var timeSlot = new TimeSlot
                {
                    SlotStart = slot.SlotStart
                };

                if (DateTime.TryParse(slot.SlotStart, out var slotTime))
                {
                    timeSlot.StartsAt = slotTime;
                }

                foreach (var room in slot.Rooms)
                {
                    if (room.Session != null)
                    {
                        var session = new Session
                        {
                            Id = room.Session.Id,
                            Title = room.Session.Title,
                            Description = room.Session.Description ?? string.Empty,
                            RoomId = room.Id.ToString(),
                            Room = room.Name,
                            IsServiceSession = room.Session.IsServiceSession,
                            IsPlenumSession = room.Session.IsPlenumSession
                        };

                        if (DateTime.TryParse(room.Session.StartsAt, out var startsAt))
                        {
                            session.StartsAt = startsAt;
                            timeSlot.StartsAt = startsAt;
                        }

                        if (DateTime.TryParse(room.Session.EndsAt, out var endsAt))
                        {
                            session.EndsAt = endsAt;
                            timeSlot.EndsAt = endsAt;
                        }

                        if (room.Session.Speakers != null)
                        {
                            foreach (var speaker in room.Session.Speakers)
                            {
                                // Try to get full speaker data from lookup
                                if (speakerLookup.TryGetValue(speaker.Id, out var fullSpeaker))
                                {
                                    session.Speakers.Add(new Speaker
                                    {
                                        Id = fullSpeaker.Id,
                                        FirstName = fullSpeaker.FirstName,
                                        LastName = fullSpeaker.LastName,
                                        FullName = fullSpeaker.FullName,
                                        ProfilePicture = fullSpeaker.ProfilePicture,
                                        Bio = fullSpeaker.Bio,
                                        TagLine = fullSpeaker.TagLine
                                    });
                                }
                                else
                                {
                                    // Fallback to minimal data from grid
                                    session.Speakers.Add(new Speaker
                                    {
                                        Id = speaker.Id,
                                        FirstName = speaker.FirstName ?? string.Empty,
                                        LastName = speaker.LastName ?? string.Empty,
                                        FullName = speaker.Name,
                                        ProfilePicture = speaker.ProfilePicture ?? string.Empty
                                    });
                                }
                            }
                        }

                        timeSlot.Add(session);
                    }
                }

                if (timeSlot.Any())
                {
                    daySchedule.TimeSlots.Add(timeSlot);
                }
            }

            if (daySchedule.TimeSlots.Any())
            {
                result.Add(daySchedule);
            }
        }

        return result;
    }

    private class GridSmartDay
    {
        public string Date { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public List<GridSmartTimeSlot> TimeSlots { get; set; } = new();
    }

    private class GridSmartTimeSlot
    {
        public string SlotStart { get; set; } = string.Empty;
        public List<GridSmartRoom> Rooms { get; set; } = new();
    }

    private class GridSmartRoom
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public GridSmartSession? Session { get; set; }
    }

    private class GridSmartSession
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartsAt { get; set; }
        public string? EndsAt { get; set; }
        public bool IsServiceSession { get; set; }
        public bool IsPlenumSession { get; set; }
        public List<GridSmartSpeaker>? Speakers { get; set; }
    }

    private class GridSmartSpeaker
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
