namespace Conference.Maui.Controls;

public partial class SessionCarouselCardView : ContentView
{
    public static readonly BindableProperty BorderStrokeProperty =
        BindableProperty.Create(nameof(BorderStroke), typeof(Brush), typeof(SessionCarouselCardView),
            new SolidColorBrush(Colors.Gray));

    public static readonly BindableProperty BorderThicknessProperty =
        BindableProperty.Create(nameof(BorderThickness), typeof(double), typeof(SessionCarouselCardView), 1.0);

    public Brush BorderStroke
    {
        get => (Brush)GetValue(BorderStrokeProperty);
        set => SetValue(BorderStrokeProperty, value);
    }

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public SessionCarouselCardView()
    {
        InitializeComponent();
    }
}
