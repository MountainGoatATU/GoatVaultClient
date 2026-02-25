using System.Windows.Input;

namespace GoatVaultClient.Controls;

public partial class ImageButton : ContentView
{
    public ImageButton()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph),
        typeof(string),
        typeof(ImageButton),
        string.Empty);
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor),
        typeof(Color),
        typeof(ImageButton),
        Colors.Transparent);
    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly BindableProperty IconBgColorHoverProperty = BindableProperty.Create(
        nameof(IconBgColorHover),
        typeof(Color),
        typeof(ImageButton),
        Colors.Transparent);
    public Color IconBgColorHover
    {
        get => (Color)GetValue(IconBgColorHoverProperty);
        set => SetValue(IconBgColorHoverProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ImageButton), null);

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}