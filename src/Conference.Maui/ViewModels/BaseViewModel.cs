using CommunityToolkit.Mvvm.ComponentModel;

namespace Conference.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private bool _isRefreshing;
}
