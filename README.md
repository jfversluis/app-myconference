# Conference Mobile App

A fully-featured, white-label conference app built with .NET MAUI, powered by Sessionize. This app is designed to be customized and released by event organizers for their own conferences.

![.NET MAUI](https://img.shields.io/badge/.NET-MAUI-purple)
![.NET 10](https://img.shields.io/badge/.NET-10-blue)
![Platform](https://img.shields.io/badge/Platform-iOS%20%7C%20Android-green)

## Features

### 📅 Sessions
- **Day-based Navigation**: Easy switching between conference days with intelligent date detection
- **Smart Search**: Filter sessions by title or speaker name
- **Sticky Headers**: Time slot headers stay visible while scrolling
- **Rich Information**: View session title, time, room, speakers, and description
- **Favorites**: Star sessions to add them to your personal schedule
- **Overlapping Speaker Images**: Beautiful Fluent Design-style profile image display for multi-speaker sessions

### 👥 Speakers
- **Speaker Directory**: Browse all conference speakers with search
- **Detailed Profiles**: View full bios, taglines, social links, and sessions
- **Smart Navigation**: Seamless navigation between sessions and speakers without infinite loops

### ⭐ My Favorites
- **Personal Schedule**: View all your favorited sessions in chronological order
- **Conflict Detection**: Visual warnings when you've favorited overlapping sessions
- **Quick Access**: Easily unfavorite or navigate to session details
- **Auto-scroll**: Automatically scrolls to current time slot during the event

### ℹ️ About & Settings
- **Event Information**: Customizable about page for your event
- **Theme Support**: Light mode, Dark mode, or System default
- **Third-party Licenses**: Attribution for open-source libraries used

### 🚀 Technical Features
- **Offline-First**: Works without internet connection using intelligent caching
- **Background Sync**: Data loads in background, shows cached data immediately
- **Resilient**: Automatic retry with exponential backoff using Polly
- **Optimized**: Uses Sessionize hash checking to minimize data transfers
- **Modern Architecture**: Clean MVVM with dependency injection
- **Responsive**: Smooth scrolling and performant UI

## Using This App for Your Event

### Prerequisites
- Visual Studio 2022 17.12+ or Visual Studio Code with C# Dev Kit
- .NET 10 SDK
- A Sessionize account with your event data

### Quick Start

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/app-myconference.git
   cd app-myconference
   ```

2. **Configure Your Sessionize ID**
   
   Open `src/Conference.Maui/AppConfig.cs` and replace the Sessionize ID:
   ```csharp
   public const string SessionizeId = "your-sessionize-id";
   ```
   
   Find your Sessionize ID from your event's API URL (e.g., `https://sessionize.com/api/v2/YOUR-ID/view/All`)

3. **Customize App Metadata**
   
   Open `src/Conference.Maui/Conference.Maui.csproj` and update:
   ```xml
   <ApplicationTitle>Your Event Name</ApplicationTitle>
   <ApplicationId>com.yourdomain.eventname</ApplicationId>
   ```

4. **Customize Branding** (Optional)
   
   - Replace app icons in `Resources/AppIcon/`
   - Update colors in `Resources/Styles/Colors.xaml`
   - Modify the about page in `ViewModels/AboutViewModel.cs`

5. **Build and Run**
   ```bash
   dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-android
   dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-ios
   ```

### Customization Guide

#### Change App Colors

Edit `src/Conference.Maui/Resources/Styles/Colors.xaml`:

```xml
<Color x:Key="Primary">#512BD4</Color>  <!-- Your primary brand color -->
<Color x:Key="Secondary">#DFD8F7</Color>  <!-- Your secondary color -->
```

#### Update About Page

Edit `src/Conference.Maui/ViewModels/AboutViewModel.cs`:

```csharp
[ObservableProperty]
private string eventName = "Your Event Name";

[ObservableProperty]
private string eventDescription = "Your event description here...";
```

#### Change App Name

The app name appears in three places:
1. **Home Screen**: Set `ApplicationTitle` in `.csproj`
2. **Tab Bar**: Automatically uses Sessionize event name
3. **About Page**: Set in `AboutViewModel.cs`

#### Add Custom Fonts

1. Add font files to `Resources/Fonts/`
2. Register in `MauiProgram.cs`:
   ```csharp
   .ConfigureFonts(fonts =>
   {
       fonts.AddFont("YourFont-Regular.ttf", "YourFontRegular");
   });
   ```
3. Use in `Resources/Styles/Styles.xaml`

## Architecture

### Technology Stack

- **.NET MAUI 10**: Cross-platform UI framework
- **CommunityToolkit.Mvvm**: MVVM source generators
- **CommunityToolkit.Maui**: Additional converters and behaviors
- **Akavache**: Reactive caching with SQLite storage
- **Polly**: Resilience and transient fault handling
- **Sessionize.Api.Client**: Sessionize API integration

### Project Structure

```
Conference.Maui/
├── AppConfig.cs                 # Single point for Sessionize ID
├── Models/                      # Data models
├── Services/                    # Business logic and data access
│   ├── SessionizeService.cs    # API integration with caching
│   └── FavoritesService.cs     # Favorites management
├── ViewModels/                  # MVVM view models
├── Views/                       # XAML pages
│   ├── Sessions/
│   ├── Speakers/
│   ├── Favorites/
│   ├── About/
│   └── Settings/
└── Resources/                   # Images, fonts, styles
```

### Data Flow

1. **App Launch**: Loads cached data immediately
2. **Background**: Checks Sessionize hash for updates
3. **If Changed**: Downloads new data with retry logic
4. **Cache**: Stores in SQLite via Akavache
5. **UI Update**: Reactive updates to all views

## Development

### Build for Android

```bash
dotnet build -f net10.0-android
```

### Build for iOS

```bash
dotnet build -f net10.0-ios
```

### Run on Android Emulator

```bash
dotnet build -t:Run -f net10.0-android
```

### Run on iOS Simulator

```bash
dotnet build -t:Run -f net10.0-ios
```

## Publishing

### Android (Google Play)

1. Update version in `.csproj`:
   ```xml
   <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
   <ApplicationVersion>1</ApplicationVersion>
   ```

2. Create a signing key
3. Build release:
   ```bash
   dotnet publish -f net10.0-android -c Release
   ```

### iOS (App Store)

1. Update version in `.csproj`
2. Configure signing in Xcode or VS
3. Build archive:
   ```bash
   dotnet publish -f net10.0-ios -c Release
   ```

## Troubleshooting

### "No data showing"
- Verify your Sessionize ID in `AppConfig.cs`
- Check that your Sessionize event has published data
- Enable API access in your Sessionize event settings

### "Build errors"
- Ensure you have .NET 10 SDK installed
- Run `dotnet workload restore`
- Clean and rebuild: `dotnet clean && dotnet build`

### "iOS Xcode version mismatch"
- Install the correct Xcode version
- Or wait for .NET update matching your Xcode version

## Contributing

While this is primarily a white-label template, improvements are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

This app is inspired by the original [Xamarin conference app](https://github.com/xamarinhq/app-conference) and builds upon the work started by [James Montemagno](https://github.com/jamesmontemagno/app-myconference).

### Third-Party Libraries

- **Akavache** - MIT License
- **Polly** - BSD-3-Clause License
- **CommunityToolkit.Mvvm** - MIT License
- **CommunityToolkit.Maui** - MIT License
- **Sessionize.Api.Client** - MIT License

### Icons

Icons are custom SVG implementations based on common design patterns.

---

**Built with ❤️ using .NET MAUI**

For questions or support, please open an issue on GitHub.