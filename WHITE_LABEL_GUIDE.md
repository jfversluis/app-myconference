# White-Label Guide

Turn this app into a branded conference companion for **any event** in minutes.

## Quick Start (5 minutes)

1. **Get a Sessionize API ID** — Go to your [Sessionize](https://sessionize.com) event → API/Integrations → copy the event ID from the "All Data" endpoint URL.

2. **Edit `src/Conference.Maui/Resources/Raw/event_config.json`** — Update at minimum:
   ```json
   {
     "event": {
       "name": "Your Conference 2025",
       "startDate": "2025-09-10",
       "endDate": "2025-09-12"
     },
     "api": {
       "sessionizeEventId": "YOUR_SESSIONIZE_ID"
     },
     "app": {
       "displayName": "YourConf",
       "bundleId": "com.yourcompany.yourconf",
       "version": "1.0"
     }
   }
   ```

3. **Run the configure script** to update build metadata:
   ```bash
   ./configure.sh
   ```

4. **Build and run:**
   ```bash
   dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-ios
   ```

That's it. The app will show your event's sessions, speakers, and schedule.

---

## Configuration Reference

All configuration lives in **one file**: `src/Conference.Maui/Resources/Raw/event_config.json`

### Event Information

| Field | Required | Description |
|-------|----------|-------------|
| `event.name` | ✅ | Full event name displayed throughout the app |
| `event.description` | | Event tagline shown on the About page |
| `event.startDate` | ✅ | First day of the event (YYYY-MM-DD) |
| `event.endDate` | ✅ | Last day of the event (YYYY-MM-DD) |
| `event.isOnline` | | Set `true` for virtual events (hides venue/map) |

### Venue

| Field | Required | Description |
|-------|----------|-------------|
| `venue.name` | | Venue name shown on About page |
| `venue.address` | | Full address shown on About page |
| `venue.latitude` | | Map pin latitude (decimal degrees) |
| `venue.longitude` | | Map pin longitude (decimal degrees) |

### API

| Field | Required | Description |
|-------|----------|-------------|
| `api.sessionizeEventId` | ✅ | Your Sessionize event ID |
| `api.sessionizeBaseUrl` | | Override base URL (default: `https://sessionize.com/api/v2/`) |
| `api.cacheExpirationHours` | | Hours before re-fetching data (default: 24) |
| `api.maxRetryAttempts` | | API retry count (default: 3) |
| `api.apiTimeoutSeconds` | | Request timeout in seconds (default: 30) |

### App Metadata

| Field | Required | Description |
|-------|----------|-------------|
| `app.displayName` | ✅ | App name shown in title bars and settings |
| `app.bundleId` | ✅ | Bundle identifier (e.g., `com.yourcompany.app`) |
| `app.version` | | Display version (e.g., `1.0`) |

### Links

| Field | Description |
|-------|-------------|
| `links.website` | Event website URL (shown in About) |
| `links.github` | Source code repository (shown in Settings) |
| `links.sessionize` | Sessionize profile link (shown in Settings) |

### WiFi

| Field | Description |
|-------|-------------|
| `wifi.networkName` | Venue WiFi SSID (shown on About page) |
| `wifi.password` | WiFi password |

Leave empty or remove to hide the WiFi card.

### Branding (Colors)

All colors are hex strings (e.g., `"#0078D4"`). The app auto-generates a dark mode variant from your primary color.

| Field | Description |
|-------|-------------|
| `branding.primaryColor` | Main brand color (buttons, headers, links) |
| `branding.primaryDark` | Darker variant for pressed states |
| `branding.primaryLight` | Lighter variant for highlights |
| `branding.primaryDeep` | Deepest variant for gradient starts |
| `branding.secondaryColor` | Secondary accent color |
| `branding.accentColor` | Highlight color for special elements |
| `branding.heroGradientStart` | Hero banner gradient start color |
| `branding.heroGradientMiddle` | Hero banner gradient middle color |
| `branding.heroGradientEnd` | Hero banner gradient end color |

**Tip:** Only `primaryColor` is needed — the app derives reasonable defaults for the rest.

### Feature Flags

Toggle entire features on/off. All default to `true`.

| Flag | Description |
|------|-------------|
| `features.enableQuickPick` | Tinder-style session discovery |
| `features.enableOnboarding` | Welcome tour on first launch |
| `features.enableReminders` | Session reminder notifications |
| `features.enableWifi` | WiFi connect card on About page |
| `features.enableSponsors` | Sponsors section on About page |
| `features.enableConflictResolver` | Schedule conflict detection |
| `features.enableHapticFeedback` | Vibration on swipes and actions |

---

## Sponsors

To show sponsors on the About page:

1. Create `src/Conference.Maui/Resources/Raw/sponsors.json`:
   ```json
   [
     {
       "name": "Acme Corp",
       "tier": "Gold",
       "logoUrl": "https://example.com/logo.png",
       "websiteUrl": "https://acmecorp.com"
     }
   ]
   ```

2. Ensure `features.enableSponsors` is `true` in `event_config.json`.

---

## Custom App Icon

Replace the icon files in:
- `src/Conference.Maui/Resources/AppIcon/appicon.svg` — main icon (SVG)
- `src/Conference.Maui/Resources/AppIcon/appiconfg.svg` — foreground layer (Android adaptive icon)

Then rebuild. The build pipeline generates all required sizes automatically.

---

## Build & Deploy

### iOS
```bash
# Simulator
dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-ios

# Physical device
dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-ios -r ios-arm64
```

### Android
```bash
dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-android
```

### Mac Catalyst
```bash
dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-maccatalyst
```

---

## Using a Custom Config Path

If you maintain multiple event configs (e.g., for different conferences):

```bash
./configure.sh --config configs/devdays2025.json
```

This copies the config into the app bundle location and updates the .csproj.

---

## Architecture Overview

```
event_config.json
    ↓ (loaded at startup)
EventConfigService (singleton)
    ↓ (injected via DI)
┌─────────────────────────────┐
│ ViewModels                  │ ← read config for display strings
│ Services                    │ ← read config for API URLs, timeouts
│ App.xaml.cs                 │ ← applies branding colors to resources
│ Pages (XAML)                │ ← bind IsVisible to feature flags
└─────────────────────────────┘
```

The config is loaded **synchronously** before any UI renders, ensuring brand colors and feature flags are applied from the very first screen.
