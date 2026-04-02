namespace Conference.Maui.Interfaces;

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
