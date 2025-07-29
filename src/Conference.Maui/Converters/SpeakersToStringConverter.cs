using Conference.Maui.Models;
using System.Collections;
using System.Globalization;

namespace Conference.Maui.Converters;

public class SpeakersToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<Speaker> speakers)
        {
            var speakerNames = speakers.Select(s => s.FullName).ToList();
            return speakerNames.Count switch
            {
                0 => "No speakers",
                1 => speakerNames[0],
                _ => string.Join(", ", speakerNames)
            };
        }

        return "No speakers";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
