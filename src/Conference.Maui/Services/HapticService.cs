namespace Conference.Maui.Services;

public interface IHapticService
{
    void Perform(HapticIntensity intensity = HapticIntensity.Medium);
}

public enum HapticIntensity
{
    Light,
    Medium,
    Heavy
}

public class HapticService : IHapticService
{
    private const string PrefKey = "haptic_feedback_enabled";

    public static bool IsEnabled
    {
        get => Preferences.Get(PrefKey, true);
        set => Preferences.Set(PrefKey, value);
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
        catch
        {
            // Haptics not available on this device/platform
        }
    }
}
