using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;

namespace Conference.Maui.Services;

public class HapticService : IHapticService
{
    public static bool IsEnabled
    {
        get => Preferences.Get(PreferenceKeys.HapticFeedbackEnabled, true);
        set => Preferences.Set(PreferenceKeys.HapticFeedbackEnabled, value);
    }

    public void Perform(HapticIntensity intensity = HapticIntensity.Medium)
    {
        if (!IsEnabled) return;

        try
        {
            var type = intensity switch
            {
                HapticIntensity.Light => HapticFeedbackType.Click,
                HapticIntensity.Heavy => HapticFeedbackType.LongPress,
                _ => HapticFeedbackType.Click
            };
            HapticFeedback.Default.Perform(type);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Haptics not available: {ex.Message}");
        }
    }
}
