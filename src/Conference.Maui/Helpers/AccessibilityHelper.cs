#if IOS || MACCATALYST
using UIKit;
#endif
#if ANDROID
using Android.Views.Accessibility;
#endif

namespace Conference.Maui.Helpers;

/// <summary>
/// Cross-platform helper for accessibility queries.
/// </summary>
public static class AccessibilityHelper
{
    /// <summary>
    /// Returns true when the user has enabled Reduce Motion (iOS) or Remove Animations (Android).
    /// Animations should be simplified or skipped when this returns true.
    /// </summary>
    public static bool ShouldReduceMotion
    {
        get
        {
#if IOS || MACCATALYST
            return UIAccessibility.IsReduceMotionEnabled;
#elif ANDROID
            var context = Platform.CurrentActivity ?? Platform.AppContext;
            var manager = (AccessibilityManager?)context.GetSystemService(Android.Content.Context.AccessibilityService);
            // Android doesn't have a direct "reduce motion" API; we approximate with animation scale
            try
            {
                var animationScale = Android.Provider.Settings.Global.GetFloat(
                    context.ContentResolver,
                    Android.Provider.Settings.Global.AnimatorDurationScale, 1.0f);
                return animationScale == 0f;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }
    }
}
