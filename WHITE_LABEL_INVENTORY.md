# White-Label Configuration Inventory
## .NET MAUI Conference App - Hardcoded Values Analysis

This document catalogs every hardcoded value that should be configurable for white-labeling this conference app.

---

## ✅ CATEGORY 1: MUST BE CONFIGURABLE
**Event-specific values that change per deployment**

### 1.1 Event Identity & Branding

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Conference Name | "NDC Copenhagen 2026" | `Configuration/AppConfig.cs:46` | `event.name` |
| App Display Name | "MyConference" | `Configuration/AppConfig.cs:25` | `app.displayName` |
| App Title (Shell) | "MyConference" | `AppShell.xaml:9` | `app.displayName` |
| App Title (Onboarding) | "MyConference" | `Pages/OnboardingPage.xaml:199` | `app.displayName` |
| App Title (Resource) | "My Conference" | `Resources/Strings/Strings.resx:121` | `app.displayName` |
| Event Description | "Four days of software development..." | `Configuration/AppConfig.cs:51` | `event.description` |

### 1.2 Event Dates & Location

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Event Start Date | `new(2026, 6, 1)` | `Configuration/AppConfig.cs:68` | `event.startDate` |
| Event End Date | `new(2026, 6, 4)` | `Configuration/AppConfig.cs:69` | `event.endDate` |
| Venue Name | "Øksnehallen" | `Configuration/AppConfig.cs:62` | `event.venue.name` |
| Venue Address | "Halmtorvet 11, 1700 København V, Denmark" | `Configuration/AppConfig.cs:63` | `event.venue.address` |
| Venue Coordinates (Latitude) | `55.6377` | `Pages/AboutPage.xaml.cs:38` | `event.venue.latitude` |
| Venue Coordinates (Longitude) | `12.5741` | `Pages/AboutPage.xaml.cs:38` | `event.venue.longitude` |
| Is Online Event | `false` | `Configuration/AppConfig.cs:57` | `event.isOnline` |

### 1.3 API Configuration

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Sessionize API ID | "5g27052o" | `Configuration/AppConfig.cs:14` | `api.sessionize.eventId` |
| Sessionize Base URL | "https://sessionize.com/api/v2/" | `Configuration/AppConfig.cs:20` | `api.sessionize.baseUrl` |

### 1.4 WiFi Configuration (Already in JSON!)

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| WiFi Network Name | "NDC-CPH-2026" | `Resources/Raw/event_config.json:3` | ✅ Already in `wifi.networkName` |
| WiFi Password | "ndccopenhagen" | `Resources/Raw/event_config.json:4` | ✅ Already in `wifi.password` |

### 1.5 Sponsors (Already in JSON!)

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Sponsors List | Array of sponsor objects | `Resources/Raw/sponsors.json` | ✅ Already configurable |

### 1.6 App Metadata & Bundle IDs

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Application Title | "MyConference" | `Conference.Maui.csproj:13` | `app.displayName` |
| Application ID (Bundle) | "com.myconference.app" | `Conference.Maui.csproj:16` | `app.bundleId` |
| Application Version | "1.0" | `Conference.Maui.csproj:19` | `app.version` |
| Application Build Number | "1" | `Conference.Maui.csproj:20` | `app.buildNumber` |

### 1.7 Links & URLs

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Conference Website | "https://ndccopenhagen.com" | `Configuration/AppConfig.cs:74` | `links.website` |
| GitHub Repository | "https://github.com/jfversluis/app-myconference" | `Configuration/AppConfig.cs:79` | `links.github` |
| Sessionize Link (About) | "https://sessionize.com" | `ViewModels/AboutViewModel.cs:187` | `links.sessionize` |
| Sessionize Link (Settings) | "https://sessionize.com" | `ViewModels/SettingsViewModel.cs:220` | `links.sessionize` |
| Sessionize Link (Settings XAML) | "https://sessionize.com" | `Pages/SettingsPage.xaml:404` | `links.sessionize` |

---

## 🟡 CATEGORY 2: SHOULD BE CONFIGURABLE
**Nice to customize but has sensible defaults**

### 2.1 Primary Branding Colors

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Primary Color (Azure Blue) | `#0078D4` | `Resources/Styles/Colors.xaml:13` | `branding.colors.primary` |
| Primary Dark | `#106EBE` | `Resources/Styles/Colors.xaml:14` | `branding.colors.primaryDark` |
| Primary Light | `#40E0FF` | `Resources/Styles/Colors.xaml:15` | `branding.colors.primaryLight` |
| Primary Deep | `#005A9E` | `Resources/Styles/Colors.xaml:16` | `branding.colors.primaryDeep` |
| Light Primary | `#0078D4` | `Resources/Styles/Colors.xaml:26` | `branding.colors.lightPrimary` |
| Dark Primary | `#40A6F7` | `Resources/Styles/Colors.xaml:27` | `branding.colors.darkPrimary` |
| Secondary (Cyan) | `#00BCF2` | `Resources/Styles/Colors.xaml:53` | `branding.colors.secondary` |
| Accent (Orange) | `#FF6900` | `Resources/Styles/Colors.xaml:57` | `branding.colors.accent` |
| Android Primary Color | `#512BD4` | `Platforms/Android/Resources/values/colors.xml:3` | `branding.colors.androidPrimary` |

### 2.2 App Icon & Splash Colors

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| App Icon Background Color | `#512BD4` | `Conference.Maui.csproj:38` | `branding.colors.iconBackground` |
| Splash Screen Background | `#512BD4` | `Conference.Maui.csproj:41` | `branding.colors.splashBackground` |
| App Icon SVG Fill | `#512BD4` | `Resources/AppIcon/appicon.svg:3` | `branding.colors.iconBackground` |
| Splash Screen SVG | ".NET" text | `Resources/Splash/splash.svg` | `branding.splashScreen` (image path) |

### 2.3 UI Text Customization

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Tab: My Event | "My Event" | `AppShell.xaml:12,14` | `ui.tabs.myEvent` |
| Tab: Sessions | "Sessions" | `AppShell.xaml:18,20` | `ui.tabs.sessions` |
| Tab: Speakers | "Speakers" | `AppShell.xaml:24,26` | `ui.tabs.speakers` |
| Tab: My Agenda | "My Agenda" | `AppShell.xaml:30,32` | `ui.tabs.myAgenda` |
| Tab: About | "About" | `AppShell.xaml:36,38` | `ui.tabs.about` |
| About: Section Header | "ABOUT THE EVENT" | `Pages/AboutPage.xaml:57` | `ui.about.sectionHeader` |
| About: Sponsors Header | "OUR SPONSORS" | `Pages/AboutPage.xaml:136` | `ui.about.sponsorsHeader` |
| About: Sponsors Subtitle | "Thank you to our amazing sponsors..." | `Pages/AboutPage.xaml:142` | `ui.about.sponsorsSubtitle` |
| WiFi: Connect Button | "Connect" | `Pages/AboutPage.xaml:121` | `ui.wifi.connectButton` |
| Toolbar: Settings | "Settings" | `Pages/AboutPage.xaml:15` | `ui.toolbar.settings` |
| Toolbar: Quick Pick | "Quick Pick" | `Pages/SessionsPage.xaml:20` | `ui.toolbar.quickPick` |
| Button: Visit Website | "Visit Website" | `Pages/AboutPage.xaml:40` | `ui.buttons.visitWebsite` |

### 2.4 Notification Text Templates

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Notification Title | "Starting Soon" | `Services/ReminderService.cs:86` | `notifications.title` |
| Notification Body Template | "{title}{roomText} · Starts in {minutesText}" | `Services/ReminderService.cs:87` | `notifications.bodyTemplate` |

### 2.5 Empty State Messages

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| My Event: No Live Sessions Title | "No live sessions right now" | `ViewModels/MyEventViewModel.cs:121` | `ui.emptyStates.noLiveSessions.title` |
| My Event: No Live Sessions Message | "This dashboard comes alive during..." | `ViewModels/MyEventViewModel.cs:124` | `ui.emptyStates.noLiveSessions.message` |
| My Event: Before Event Title | "The event hasn't started yet" | `ViewModels/MyEventViewModel.cs:504` | `ui.emptyStates.beforeEvent.title` |
| My Event: After Event Title | "Thanks for attending!" | `ViewModels/MyEventViewModel.cs:510` | `ui.emptyStates.afterEvent.title` |
| My Event: Break Time Title | "Break time" | `ViewModels/MyEventViewModel.cs:523` | `ui.emptyStates.breakTime.title` |
| Favorites: Empty Title | "No sessions in your agenda yet" | `Pages/FavoritesPage.xaml:164` | `ui.emptyStates.noFavorites.title` |
| Favorites: Empty Message | "Browse sessions and tap the star icon..." | `Pages/FavoritesPage.xaml:169` | `ui.emptyStates.noFavorites.message` |
| Quick Pick: Build Agenda | "Build your personal agenda" | `Pages/MyEventPage.xaml:356` | `ui.quickPick.buildAgenda.title` |
| Quick Pick: Build Agenda Subtitle | "Tap the star on sessions..." | `Pages/MyEventPage.xaml:358` | `ui.quickPick.buildAgenda.subtitle` |

### 2.6 Onboarding Flow Text

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Welcome: Greeting Prefix | "Welcome to " | `Pages/OnboardingPage.xaml:218` | `onboarding.welcome.prefix` |
| Welcome: Get Started Button | "Get Started" | `Pages/OnboardingPage.xaml:231` | `onboarding.welcome.getStartedButton` |
| Welcome: Skip Button | "Skip, I'll explore myself" | `Pages/OnboardingPage.xaml:243` | `onboarding.welcome.skipButton` |
| Notifications: Title | "Stay in the loop" | `Pages/OnboardingPage.xaml:270` | `onboarding.notifications.title` |
| Notifications: Description | "Get reminders before your sessions..." | `Pages/OnboardingPage.xaml:283` | `onboarding.notifications.description` |
| Notifications: Privacy Note | "We'll only notify you about..." | `Pages/OnboardingPage.xaml:289` | `onboarding.notifications.privacyNote` |
| Notifications: Enable Button | "Enable Notifications" | `Pages/OnboardingPage.xaml:296` | `onboarding.notifications.enableButton` |
| Notifications: Success Message | "✅ Notifications enabled — you're all set!" | `Pages/OnboardingPage.xaml:307` | `onboarding.notifications.successMessage` |
| Interests: Title | "What interests you?" | `Pages/OnboardingPage.xaml:350` | `onboarding.interests.title` |
| Interests: Subtitle | "Pick your topics and we'll recommend..." | `Pages/OnboardingPage.xaml:354` | `onboarding.interests.subtitle` |
| Complete: Title | "You're all set!" | `Pages/OnboardingPage.xaml:670` | `onboarding.complete.title` |
| Complete: Message | "Your personalized agenda is ready..." | `Pages/OnboardingPage.xaml:689` | `onboarding.complete.message` |

### 2.7 Conflict Resolution Text

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Conflicts: Title | "Resolve Conflicts" | `Pages/ConflictResolverPage.xaml:20` | `ui.conflicts.title` |
| Conflicts: Instruction | "Tap 'Keep' on the session you want..." | `Pages/ConflictResolverPage.xaml:175` | `ui.conflicts.instruction` |
| Conflicts: All Resolved Title | "All conflicts resolved!" | `Pages/ConflictResolverPage.xaml:57` | `ui.conflicts.resolvedTitle` |
| Conflicts: All Resolved Message | "Your agenda is now conflict-free." | `Pages/ConflictResolverPage.xaml:59` | `ui.conflicts.resolvedMessage` |
| Conflicts: View Agenda Button | "View My Agenda" | `Pages/ConflictResolverPage.xaml:63` | `ui.conflicts.viewAgendaButton` |

### 2.8 Settings Page Text

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Settings: Notifications Section | "NOTIFICATIONS" | `Pages/SettingsPage.xaml:22` | `ui.settings.sections.notifications` |
| Settings: Preferences Section | "PREFERENCES" | `Pages/SettingsPage.xaml:129` | `ui.settings.sections.preferences` |
| Settings: App Info Section | "APP INFO" | `Pages/SettingsPage.xaml:273` | `ui.settings.sections.appInfo` |
| Settings: Libraries Section | "LIBRARIES" | `Pages/SettingsPage.xaml:317` | `ui.settings.sections.libraries` |
| Settings: Links Section | "LINKS" | `Pages/SettingsPage.xaml:419` | `ui.settings.sections.links` |
| Settings: Built With Text | "Built with" | `Pages/SettingsPage.xaml:307` | `ui.settings.builtWith` |
| Settings: Framework | ".NET MAUI" | `ViewModels/SettingsViewModel.cs:21` | `ui.settings.framework` |
| Settings: View Source Code | "View Source Code" | `Pages/SettingsPage.xaml:441` | `ui.settings.viewSourceCode` |
| Settings: Open Source Subtitle | "Open-source on GitHub" | `Pages/SettingsPage.xaml:442` | `ui.settings.openSourceSubtitle` |
| Settings: Powered By Sessionize | "Powered by Sessionize" | `Pages/SettingsPage.xaml:466` | `ui.settings.poweredBySessionize` |

### 2.9 API & Performance Settings

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Cache Expiration Hours | `24` | `Configuration/AppConfig.cs:31` | `api.cacheExpirationHours` |
| Max Retry Attempts | `3` | `Configuration/AppConfig.cs:36` | `api.maxRetryAttempts` |
| API Timeout Seconds | `30` | `Configuration/AppConfig.cs:41` | `api.timeoutSeconds` |

### 2.10 Feature Flags

| Item | Current Value | Location | Suggested Config Key |
|------|---------------|----------|---------------------|
| Show WiFi Section | Implicit (always shown if in config) | `Pages/AboutPage.xaml` | `features.wifi.enabled` |
| Show Sponsors Section | Implicit (always shown if in JSON) | `Pages/AboutPage.xaml` | `features.sponsors.enabled` |
| Enable Quick Pick | Implicit (always shown) | `Pages/SessionsPage.xaml:19` | `features.quickPick.enabled` |
| Enable Onboarding | Checked via preferences | `ViewModels/OnboardingViewModel.cs` | `features.onboarding.enabled` |
| Enable Notifications | Configurable in settings | `ViewModels/SettingsViewModel.cs` | `features.notifications.enabled` |
| Default Reminder Lead Time | `15` minutes | `Services/ReminderService.cs:26` | `features.notifications.defaultLeadTimeMinutes` |
| Enable Haptics | Configurable in settings | `Services/HapticService.cs` | `features.haptics.enabled` |

---

## ✅ CATEGORY 3: CAN STAY HARDCODED
**Generic UI text and framework constants that don't need customization**

### 3.1 Generic UI Labels (Not Event-Specific)

- Search placeholders ("Search sessions...", "Search speakers...")
- Loading states ("Loading sessions...", "Loading speakers...")
- Error messages ("Couldn't load sessions", "Check your connection...")
- Time formats and duration displays
- Generic button labels ("Keep", "Skip", "Done", "Close", "Retry")
- Separator characters ("·", " · ")
- Accessibility descriptions
- Semantic properties

### 3.2 Technical Constants

- XML namespaces in XAML files
- MAUI schema URLs
- Resource file schemas
- Cache key constants (internal naming)
- Notification category types
- Platform-specific constants

### 3.3 Library/Framework References

- Package names and versions (`Conference.Maui.csproj`)
- Library attributions (MAUI Community Toolkit, Syncfusion, Akavache, Polly)
- NuGet package references
- Platform SDK versions

### 3.4 Gray Scale & Semantic Colors

All grayscale colors (Gray50-Gray950) are generic UI colors and don't need event branding:
- `Resources/Styles/Colors.xaml:90-113` - Gray scale palette
- Background, Surface, Border colors
- Text colors (Primary, Secondary, Tertiary)
- Light/Dark theme surface colors

### 3.5 Status/Warning Colors

These are semantic colors with universal meanings:
- `LikeColor` (#63DD99) - Green for approve
- `DislikeColor` (#FF6A4F) - Red for reject
- `WarningLight`, `WarningDark` - Warning states
- `ErrorRed` (#D32F2F) - Error states
- `ConflictOrange` - Scheduling conflicts

---

## 📋 RECOMMENDED CONFIGURATION SCHEMA

Based on this analysis, here's the suggested JSON structure for `event_config.json`:

```json
{
  "app": {
    "displayName": "MyConference",
    "bundleId": "com.myconference.app",
    "version": "1.0",
    "buildNumber": "1"
  },
  "event": {
    "name": "NDC Copenhagen 2026",
    "description": "Four days of software development talks and workshops...",
    "startDate": "2026-06-01",
    "endDate": "2026-06-04",
    "isOnline": false,
    "venue": {
      "name": "Øksnehallen",
      "address": "Halmtorvet 11, 1700 København V, Denmark",
      "latitude": 55.6377,
      "longitude": 12.5741
    }
  },
  "api": {
    "sessionize": {
      "eventId": "5g27052o",
      "baseUrl": "https://sessionize.com/api/v2/"
    },
    "cacheExpirationHours": 24,
    "maxRetryAttempts": 3,
    "timeoutSeconds": 30
  },
  "links": {
    "website": "https://ndccopenhagen.com",
    "github": "https://github.com/jfversluis/app-myconference",
    "sessionize": "https://sessionize.com"
  },
  "branding": {
    "colors": {
      "primary": "#0078D4",
      "primaryDark": "#106EBE",
      "primaryLight": "#40E0FF",
      "primaryDeep": "#005A9E",
      "lightPrimary": "#0078D4",
      "darkPrimary": "#40A6F7",
      "secondary": "#00BCF2",
      "accent": "#FF6900",
      "iconBackground": "#512BD4",
      "splashBackground": "#512BD4",
      "androidPrimary": "#512BD4"
    },
    "assets": {
      "appIcon": "Resources/AppIcon/appicon.svg",
      "splashScreen": "Resources/Splash/splash.svg"
    }
  },
  "wifi": {
    "networkName": "NDC-CPH-2026",
    "password": "ndccopenhagen"
  },
  "features": {
    "wifi": { "enabled": true },
    "sponsors": { "enabled": true },
    "quickPick": { "enabled": true },
    "onboarding": { "enabled": true },
    "notifications": {
      "enabled": true,
      "defaultLeadTimeMinutes": 15
    },
    "haptics": { "enabled": true }
  },
  "ui": {
    "tabs": {
      "myEvent": "My Event",
      "sessions": "Sessions",
      "speakers": "Speakers",
      "myAgenda": "My Agenda",
      "about": "About"
    },
    "notifications": {
      "title": "Starting Soon",
      "bodyTemplate": "{title}{roomText} · Starts in {minutesText}"
    }
  }
}
```

---

## 🎯 IMPLEMENTATION PRIORITY

### Phase 1: Critical (Must-Have for White Label)
1. Event identity (name, dates, venue)
2. API configuration (Sessionize ID)
3. App metadata (bundle ID, display name)
4. Primary branding colors
5. Links (website, GitHub)

### Phase 2: Important (Should-Have)
1. WiFi configuration (already JSON ✅)
2. Sponsors (already JSON ✅)
3. App icon/splash colors
4. Feature flags (WiFi, sponsors, Quick Pick)
5. Notification text templates

### Phase 3: Nice-to-Have
1. All UI text customization
2. Empty state messages
3. Onboarding flow text
4. Settings page text
5. Complete theme customization

---

## 🔍 NOTES

### Already Configurable via JSON
- ✅ WiFi settings (`Resources/Raw/event_config.json`)
- ✅ Sponsors list (`Resources/Raw/sponsors.json`)

### Partially Configurable
- Some values in `Configuration/AppConfig.cs` (static constants)
- Need to migrate to JSON for runtime configuration

### Requires Code Generation
- App bundle ID (`Conference.Maui.csproj:16`)
- App display name (`Conference.Maui.csproj:13`)
- Platform-specific files (AndroidManifest.xml, Info.plist)
- These would need build-time generation from config

### Color System Insights
- Two color themes detected:
  - Azure Blue (#0078D4) - Current theme
  - Purple (#512BD4) - Used in icons/splash, possibly legacy
- Recommend: Use purple as the "default" generic branding color since it's in icons
- Azure blue appears to be NDC-specific customization

### Comments Contain Branding
- `Resources/Styles/Colors.xaml:10` - "Azure Dev Summit Inspired Color Palette"
- `Resources/Styles/Styles.xaml:196` - "Azure Dev Summit Inspired Design System"
- `Pages/AboutPage.xaml.cs:38` - "Bella Center Copenhagen"
- These comments should be updated or made generic

---

## 🚀 NEXT STEPS

1. **Design unified config schema** - Consolidate `event_config.json`, `sponsors.json`, and `AppConfig.cs` values
2. **Create config loader service** - Load and validate JSON at startup
3. **Refactor ViewModels** - Inject config instead of using static `AppConfig`
4. **Generate platform files** - Create build task to populate `.csproj`, `Info.plist`, `AndroidManifest.xml` from config
5. **Asset pipeline** - Support custom icons/splash screens via config paths
6. **Color theming system** - Dynamic color loading from config with fallbacks
7. **Validation** - Schema validation for config JSON
8. **Documentation** - White-label deployment guide for organizers
9. **Template project** - Create starter template with placeholder values
10. **CI/CD support** - Environment-based config injection for automated builds

---

Generated: 2025-03-26
Source: /Users/jfversluis/Documents/GitHub/app-myconference/src/Conference.Maui/
