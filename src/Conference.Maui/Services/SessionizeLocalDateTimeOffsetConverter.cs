using System.Globalization;
using System.Text.RegularExpressions;
using Conference.Maui.Interfaces;
using Newtonsoft.Json;

namespace Conference.Maui.Services;

public sealed partial class SessionizeLocalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private readonly IEventTimeService _eventTimeService;

    public SessionizeLocalDateTimeOffsetConverter(IEventTimeService eventTimeService)
    {
        _eventTimeService = eventTimeService;
    }

    public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return default;

        if (reader.TokenType == JsonToken.String && reader.Value is string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            if (!HasExplicitOffsetRegex().IsMatch(value))
            {
                var localClockTime = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
                var unspecified = DateTime.SpecifyKind(localClockTime, DateTimeKind.Unspecified);
                return new DateTimeOffset(unspecified, _eventTimeService.EventTimeZone.GetUtcOffset(unspecified));
            }

            return _eventTimeService.ToEventTime(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        if (reader.TokenType == JsonToken.Date)
        {
            if (reader.Value is DateTimeOffset dateTimeOffset)
                return _eventTimeService.ToEventTime(dateTimeOffset);

            if (reader.Value is DateTime dateTime)
            {
                var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                return new DateTimeOffset(unspecified, _eventTimeService.EventTimeZone.GetUtcOffset(unspecified));
            }
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Sessionize DateTimeOffset.");
    }

    public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString("o", CultureInfo.InvariantCulture));
    }

    [GeneratedRegex("(Z|[+-]\\d{2}:\\d{2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex HasExplicitOffsetRegex();
}
