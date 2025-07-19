using Conference.Maui.Models;

namespace Conference.Maui.Selectors;

public class ScheduleTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TimeHeaderTemplate { get; set; }
    public DataTemplate? SessionTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            TimeHeader => TimeHeaderTemplate ?? throw new InvalidOperationException("TimeHeaderTemplate is null"),
            Session => SessionTemplate ?? throw new InvalidOperationException("SessionTemplate is null"),
            _ => throw new ArgumentException($"Unsupported item type: {item?.GetType()}")
        };
    }
}
