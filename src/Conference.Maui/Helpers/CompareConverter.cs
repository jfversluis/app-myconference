using System.Globalization;

namespace Conference.Maui.Helpers;

public class CompareConverter : IValueConverter
{
    public enum ComparisonOperatorType
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    public ComparisonOperatorType ComparisonOperator { get; set; } = ComparisonOperatorType.Equal;
    public object? ComparingValue { get; set; }
    public object? TrueObject { get; set; } = true;
    public object? FalseObject { get; set; } = false;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || ComparingValue == null)
            return FalseObject;

        var result = false;
        var comparison = System.Convert.ToDouble(value) - System.Convert.ToDouble(ComparingValue);

        result = ComparisonOperator switch
        {
            ComparisonOperatorType.Equal => comparison == 0,
            ComparisonOperatorType.NotEqual => comparison != 0,
            ComparisonOperatorType.Greater => comparison > 0,
            ComparisonOperatorType.GreaterOrEqual => comparison >= 0,
            ComparisonOperatorType.Less => comparison < 0,
            ComparisonOperatorType.LessOrEqual => comparison <= 0,
            _ => false
        };

        return result ? TrueObject : FalseObject;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
