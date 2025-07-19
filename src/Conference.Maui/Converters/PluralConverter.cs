using System.Globalization;

namespace Conference.Maui.Converters;

public class PluralConverter : IValueConverter
{
    public string SingularFormat { get; set; } = "{0} session";
    public string PluralFormat { get; set; } = "{0} sessions";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 1 
                ? string.Format(SingularFormat, count)
                : string.Format(PluralFormat, count);
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
