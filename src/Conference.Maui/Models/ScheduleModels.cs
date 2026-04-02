using CommunityToolkit.Mvvm.ComponentModel;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.Models;

/// <summary>
/// Represents a day in the schedule with its sessions grouped by time slot.
/// </summary>
public partial class ScheduleDay : ObservableObject
{
    public DateOnly Date { get; set; }
    public string DateDisplay => Date.ToString("ddd, MMM d");
    public string DateShort => Date.ToString("MMM d");
    public string DisplayName => $"{Date:ddd} {Date.Day} {Date:MMM}";  // e.g., "Wed 10 Sep" - localized by .NET
    public List<TimeSlotGroup> TimeSlots { get; set; } = [];

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents a time slot with all sessions happening at that time.
/// </summary>
public class TimeSlotGroup : List<SessionItem>
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateOnly? Date { get; set; }
    
    public string TimeDisplay => Date.HasValue 
        ? $"{Date.Value:ddd} {Date.Value.Day} {Date.Value:MMM} · {StartTime.LocalDateTime:h:mm tt} - {EndTime.LocalDateTime:h:mm tt}"
        : $"{StartTime.LocalDateTime:h:mm tt} - {EndTime.LocalDateTime:h:mm tt}";

    public bool HasConflict => Count > 1;
    public string ConflictText => $"{Count} sessions";
}

/// <summary>
/// UI-friendly wrapper around a session with additional properties.
/// </summary>
public partial class SessionItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? RoomName { get; set; }
    public int RoomId { get; set; }
    public List<SpeakerItem> Speakers { get; set; } = [];

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _hasReminder;

    public string TimeDisplay => $"{StartsAt.LocalDateTime:h:mm tt} - {EndsAt.LocalDateTime:h:mm tt}";
    public string DurationDisplay => $"{(EndsAt - StartsAt).TotalMinutes:0} min";
    public bool HasMultipleSpeakers => Speakers.Count > 1;
    public string SpeakerNames => string.Join(", ", Speakers.Select(s => s.FullName));
    
    // Avatar display properties - show up to 4 speakers, then "+N"
    public SpeakerItem? Speaker1 => Speakers.Count > 0 ? Speakers[0] : null;
    public SpeakerItem? Speaker2 => Speakers.Count > 1 ? Speakers[1] : null;
    public SpeakerItem? Speaker3 => Speakers.Count > 2 ? Speakers[2] : null;
    public SpeakerItem? Speaker4 => Speakers.Count > 3 ? Speakers[3] : null;
    public bool HasSpeaker1 => Speakers.Count > 0;
    public bool HasSpeaker2 => Speakers.Count > 1;
    public bool HasSpeaker3 => Speakers.Count > 2;
    public bool HasSpeaker4 => Speakers.Count > 3;
    public bool HasMoreSpeakers => Speakers.Count > 4;
    public string MoreSpeakersText => $"+{Speakers.Count - 4}";
    public bool HasExtraSpeakers => Speakers.Count > 2;
    public string ExtraSpeakersText => $"+{Speakers.Count - 2}";
}

/// <summary>
/// UI-friendly wrapper around a speaker.
/// </summary>
public class SpeakerItem
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? TagLine { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public List<int> SessionIds { get; set; } = [];

    public string Initials => $"{(FirstName?.Length > 0 ? FirstName[0] : ' ')}{(LastName?.Length > 0 ? LastName[0] : ' ')}".Trim().ToUpperInvariant();

    public string BioPreview => string.IsNullOrEmpty(Bio) 
        ? string.Empty 
        : Bio.Length > 150 ? Bio[..150] + "..." : Bio;

    public static SpeakerItem FromSpeakerDetails(SpeakerDetails speaker)
    {
        return new SpeakerItem
        {
            Id = speaker.Id,
            FirstName = speaker.FirstName,
            LastName = speaker.LastName,
            FullName = speaker.FullName,
            TagLine = speaker.TagLine,
            Bio = speaker.Bio,
            ProfilePictureUrl = speaker.ProfilePicture,
            SessionIds = speaker.Sessions?.ToList() ?? []
        };
    }
}
