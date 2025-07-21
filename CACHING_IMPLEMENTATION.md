# Local Caching Implementation

## Overview

This implementation adds local caching and database functionality to the Conference app, addressing the requirements specified in issue #9. The solution provides:

- Local SQLite database storage for event data
- Hash-based cache invalidation using Sessionize API
- Background refresh with UI responsiveness
- Favorite session preservation during data updates
- Pull-to-refresh functionality
- Loading indicators

## Architecture

### Database Models

**CachedSession** - Stores session data locally
- Includes all session properties with JSON serialization for complex fields
- Conversion methods to/from Session model

**CachedSpeaker** - Stores speaker data locally  
- Handles speaker information with safe JSON serialization
- Maintains speaker-session relationships

**CachedRoom** - Stores room data locally
- Simple room information storage

**DataCacheInfo** - Stores cache metadata
- Hash values for change detection
- Last update and check timestamps
- Refresh state tracking

### Services

**DatabaseService (Enhanced)**
- Extended to handle all cached data types
- CRUD operations for cached entities
- Database initialization and table creation

**SessionizeService (Enhanced)**
- Hash-based cache validation using `?hashOnly=true` API
- Background refresh every 5 minutes
- Fallback to cached data on network errors
- Events for data refresh notifications

### ViewModels

**ScheduleViewModel & SpeakersViewModel (Enhanced)**
- Loading state management (IsLoading, IsRefreshing)
- Refresh commands for pull-to-refresh
- Event subscription for real-time updates
- IDisposable implementation for memory leak prevention
- Thread-safe UI updates

## Key Features

### 1. Hash-Based Caching
```csharp
// Checks Sessionize API for data changes
var remoteHash = await GetRemoteHashAsync();
// Only refreshes if hash differs from cached version
```

### 2. Background Refresh
- Automatic check every 5 minutes
- Non-blocking UI updates
- Preserves user interactions during refresh

### 3. Offline Support
- App works with cached data when offline
- Graceful degradation when network fails
- Data persists between app sessions

### 4. Favorite Preservation
- Favorite sessions maintained during refresh
- Local database preserves user preferences
- FavoriteSession table remains intact during updates

### 5. UI Responsiveness
- Loading indicators during initial load
- Pull-to-refresh on all data views
- Background updates don't block UI

## Usage

### For Developers

The caching is transparent to existing code. ViewModels continue to call:

```csharp
var sessions = await _eventService.GetAllSessions();
var speakers = await _eventService.GetAllSpeakers();
```

The service automatically:
1. Returns cached data immediately if available
2. Starts background refresh check
3. Updates UI when new data arrives

### For Users

- App loads instantly with cached data
- Pull down to refresh manually
- Data updates automatically in background
- Favorites are preserved across updates

## Implementation Details

### Cache Strategy
1. **First Load**: Fetch from API, cache locally
2. **Subsequent Loads**: Return cached data, check for updates
3. **Updates Available**: Download new data, update cache, notify UI
4. **Network Error**: Continue with cached data

### Threading
- Database operations on background threads
- UI updates marshaled to main thread
- Non-blocking background refresh

### Error Handling
- Network failures gracefully handled
- Database errors logged but don't crash app
- JSON serialization includes fallbacks
- Missing data defaults to empty collections

### Memory Management
- ViewModels implement IDisposable
- Event subscriptions properly cleaned up
- Prevents memory leaks from service events

## Configuration

The implementation uses these constants (configurable):

```csharp
private const string API_BASE_URL = "https://sessionize.com/api/v2/5g27052o";
private const string CACHE_KEY = "event_data";
private const int REFRESH_CHECK_MINUTES = 5;
```

## Testing

To test the implementation:

1. **Initial Load**: First app launch should fetch and cache data
2. **Offline Mode**: Disable network, app should work with cached data
3. **Pull to Refresh**: Pull down on lists to trigger manual refresh
4. **Background Updates**: Wait 5+ minutes, data should update automatically
5. **Favorites**: Add favorites, refresh data, verify favorites preserved

## Database Location

SQLite database stored at:
```
FileSystem.AppDataDirectory/ConferenceApp.db3
```

Tables created:
- CachedSessions
- CachedSpeakers  
- CachedRooms
- DataCache
- FavoriteSession (existing)

## Troubleshooting

Enable debug logging to see cache operations:
```csharp
System.Diagnostics.Debug.WriteLine("Cache operation details");
```

Common issues:
- Network timeouts: App continues with cached data
- Database corruption: Will recreate tables on next launch
- Hash API changes: Will force full refresh
- Memory usage: ViewModels dispose properly when pages close

## Future Enhancements

Possible improvements:
- Cache compression for large datasets
- Incremental updates instead of full refresh
- Cache expiration policies
- Offline analytics for cache hit rates
- Image caching for speaker photos