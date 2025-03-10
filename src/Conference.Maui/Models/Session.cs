using System.Text.Json.Serialization;

namespace Conference.Maui.Models;

public class Session
{
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("startsAt")]
        public DateTime StartsAt { get; set; }

        [JsonPropertyName("endsAt")]
        public DateTime EndsAt { get; set; }

        [JsonIgnore]
        public int DurationInMinutes => (int)EndsAt.Subtract(StartsAt).TotalMinutes;

        [JsonPropertyName("isServiceSession")]
        public bool IsServiceSession { get; set; }

        [JsonPropertyName("isPlenumSession")]
        public bool IsPlenumSession { get; set; }

        [JsonPropertyName("speakers")]
        public List<string> SpeakerIds { get; set; } = [];

        [JsonIgnore]
        public List<Speaker> Speakers { get; set; } = [];

        [JsonIgnore]
        public string SpeakerProfilePicture => Speakers?.FirstOrDefault()?.ProfilePicture ?? string.Empty;

        [JsonPropertyName("categoryItems")]
        public List<object> CategoryItems { get; set; } = [];

        [JsonPropertyName("questionAnswers")]
        public List<object> QuestionAnswers { get; set; } = [];

        [JsonPropertyName("roomId")]
        public int RoomId { get; set; }

        public string Room { get; set; } = string.Empty;

        [JsonPropertyName("liveUrl")]
        public string LiveUrl { get; set; } = string.Empty;

        [JsonPropertyName("recordingUrl")]
        public string RecordingUrl { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("isInformed")]
        public bool IsInformed { get; set; }

        [JsonPropertyName("isConfirmed")]
        public bool IsConfirmed { get; set; }
}
