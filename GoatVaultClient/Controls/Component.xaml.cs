namespace GoatVaultClient.Controls;

public partial class Component : ContentView
{
    public Component()
    {
        InitializeComponent();
    }

    // --- Original Component Properties ---

    public static readonly BindableProperty BorderMarginProperty = BindableProperty.Create(
        nameof(BorderMargin), typeof(Thickness), typeof(Component), default(Thickness));

    public Thickness BorderMargin
    {
        get => (Thickness)GetValue(BorderMarginProperty);
        set => SetValue(BorderMarginProperty, value);
    }

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(Thickness), typeof(Component), new Thickness(1));

    public Thickness StrokeThickness
    {
        get => (Thickness)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly BindableProperty BgColorProperty = BindableProperty.Create(
        nameof(BgColor), typeof(Color), typeof(Component), Colors.Transparent);

    public Color BgColor
    {
        get => (Color)GetValue(BgColorProperty);
        set => SetValue(BgColorProperty, value);
    }

    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor), typeof(Color), typeof(Component), Colors.Transparent);

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    // --- Merged Header Properties ---

    public static readonly BindableProperty ShowHeaderProperty = BindableProperty.Create(
        nameof(ShowHeader), typeof(bool), typeof(Component), false); // Defaults to hidden

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public static readonly BindableProperty ShowFooterProperty = BindableProperty.Create(
        nameof(ShowFooter), typeof(bool), typeof(Component), false); // Defaults to hidden

    public bool ShowFooter
    {
        get => (bool)GetValue(ShowFooterProperty);
        set => SetValue(ShowFooterProperty, value);
    }

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(Component), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty HeaderActionsProperty = BindableProperty.Create(
        nameof(HeaderActions), typeof(View), typeof(Component));

    public View HeaderActions
    {
        get => (View)GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }

    public static readonly BindableProperty HeaderBgColorProperty = BindableProperty.Create(
        nameof(HeaderBgColor), typeof(Color), typeof(Component), Colors.Transparent);

    public Color HeaderBgColor
    {
        get => (Color)GetValue(HeaderBgColorProperty);
        set => SetValue(HeaderBgColorProperty, value);
    }

    public static readonly BindableProperty FooterActionsProperty = BindableProperty.Create(
        nameof(FooterActions), typeof(View), typeof(Component));

    public View FooterActions
    {
        get => (View)GetValue(FooterActionsProperty);
        set => SetValue(FooterActionsProperty, value);
    }

    public static readonly BindableProperty FooterBgColorProperty = BindableProperty.Create(
        nameof(FooterBgColor), typeof(Color), typeof(Component), Colors.Transparent);

    public Color FooterBgColor
    {
        get => (Color)GetValue(FooterBgColorProperty);
        set => SetValue(FooterBgColorProperty, value);
    }
}