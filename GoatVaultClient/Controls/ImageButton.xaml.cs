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

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color),
        typeof(string),
        typeof(ImageButton),
        string.Empty);
    public string Color
    {
        get => (string)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ImageButton));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}