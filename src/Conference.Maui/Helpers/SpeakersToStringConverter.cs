using Conference.Maui.Models;
using System.Globalization;

namespace Conference.Maui.Helpers;

public class SpeakersToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<Speaker> speakers && speakers.Any())
        {
            return string.Join(", ", speakers.Select(s => s.FullName));
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
