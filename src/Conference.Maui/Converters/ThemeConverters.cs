using System.Globalization;

namespace Conference.Maui.Converters;

/// <summary>
/// Returns the Primary color when the selected index matches the parameter, Gray200/Gray700 otherwise.
/// </summary>
public class IndexToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is string paramStr && int.TryParse(paramStr, out var index))
        {
            if (selected == index)
                return Application.Current?.Resources["Primary"] as Color ?? Colors.Blue;

            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? Application.Current?.Resources["Gray800"] as Color ?? Colors.DarkGray
                : Application.Current?.Resources["Gray100"] as Color ?? Colors.LightGray;
        }
        return Colors.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

/// <summary>
/// Returns White text when the selected index matches (active button), otherwise default text color.
/// </summary>
public class IndexToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is string paramStr && int.TryParse(paramStr, out var index))
        {
            if (selected == index)
                return Colors.White;

            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? Application.Current?.Resources["Gray300"] as Color ?? Colors.LightGray
                : Application.Current?.Resources["Gray600"] as Color ?? Colors.DarkGray;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
