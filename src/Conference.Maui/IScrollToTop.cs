namespace Conference.Maui;

/// <summary>
/// Implemented by pages that support "tap active tab to scroll to top" on iOS and Android.
/// </summary>
public interface IScrollToTop
{
    void ScrollToTop();
}
