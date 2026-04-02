using CommunityToolkit.Mvvm.ComponentModel;

namespace Conference.Maui.Models;

public partial class InterestTag : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SessionCount { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}
