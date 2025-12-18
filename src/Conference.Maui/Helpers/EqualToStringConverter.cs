using System.Globalization;

namespace Conference.Maui.Helpers;

public class EqualToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string strValue && parameter is string paramString)
        {
            return strValue.Equals(paramString, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string paramString)
        {
            return paramString;
        }
        return "System";
    }
}
