using System.Globalization;

namespace Conference.Maui.Converters;

/// <summary>
/// Converts IsSelected boolean to background color for day selector pills
/// </summary>
public class SelectedDayBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current?.Resources["Primary"] ?? Colors.Blue;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsSelected boolean to text color for day selector
/// </summary>
public class SelectedDayTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Colors.White;
        }
        
        if (Application.Current?.RequestedTheme == AppTheme.Dark)
        {
            return Application.Current.Resources["Gray300"];
        }
        
        return Application.Current?.Resources["Gray600"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsSelected boolean to font attributes for day selector
/// </summary>
public class SelectedDayFontConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return FontAttributes.Bold;
        }
        return FontAttributes.None;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
