using Conference.Maui.Configuration;
using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class AboutPage : ContentPage
{
    private readonly AboutViewModel _viewModel;

    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        SetupVenueTap();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        BuildSponsorCards();
    }

    private void SetupVenueTap()
    {
        if (AppConfig.IsOnlineEvent) return;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnVenueTapped;
        VenueCard.GestureRecognizers.Add(tapGesture);
    }

    private async void OnVenueTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var location = new Location(55.6377, 12.5741); // Bella Center Copenhagen
            var options = new MapLaunchOptions
            {
                Name = AppConfig.VenueName,
                NavigationMode = NavigationMode.None
            };
            await Map.Default.OpenAsync(location, options);
        }
        catch
        {
            var query = Uri.EscapeDataString(AppConfig.VenueDetails);
            await Browser.OpenAsync($"https://maps.apple.com/?q={query}", BrowserLaunchMode.SystemPreferred);
        }
    }

    private void BuildSponsorCards()
    {
        if (SponsorsLayout.Children.Count > 0) return;

        foreach (var sponsor in _viewModel.Sponsors)
        {
            var image = new Image
            {
                Source = sponsor.ImageUrl,
                Aspect = Aspect.AspectFit,
                HeightRequest = 50,
                WidthRequest = 110,
                HorizontalOptions = LayoutOptions.Center
            };
            SemanticProperties.SetDescription(image, "");

            var nameLabel = new Label
            {
                Text = sponsor.Name,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            stack.Children.Add(image);
            stack.Children.Add(nameLabel);

            var border = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 1,
                Padding = new Thickness(12),
                Margin = new Thickness(4),
                WidthRequest = 155,
                HeightRequest = 120,
                Content = stack
            };

            // Apply theme colors
            border.SetAppThemeColor(Border.StrokeProperty,
                (Color)Application.Current!.Resources["Gray200"],
                (Color)Application.Current!.Resources["Gray600"]);
            border.SetAppThemeColor(Border.BackgroundProperty,
                Colors.White,
                (Color)Application.Current!.Resources["Gray900"]);

            SemanticProperties.SetDescription(border, $"Sponsor: {sponsor.Name}");

            if (!string.IsNullOrEmpty(sponsor.Website))
            {
                SemanticProperties.SetHint(border, "Double tap to visit sponsor website");
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    await Browser.OpenAsync(sponsor.Website, BrowserLaunchMode.SystemPreferred);
                };
                border.GestureRecognizers.Add(tapGesture);
            }

            SponsorsLayout.Children.Add(border);
        }
    }
}
