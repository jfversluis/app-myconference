using System.Globalization;

namespace Conference.Maui.Helpers;

public class BoolToObjectConverter : IValueConverter
{
    public object? TrueObject { get; set; }
    public object? FalseObject { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueObject : FalseObject;
        }
        return FalseObject;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
