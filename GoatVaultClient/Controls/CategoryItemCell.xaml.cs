using System.Windows.Input;

namespace GoatVaultClient.Controls;

public partial class CategoryItemCell
{
    public CategoryItemCell() => InitializeComponent();

    // 1. MAIN TEXT (Heading)
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(CategoryItemCell), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty EntryCountProperty = BindableProperty.Create(
        nameof(EntryCount), typeof(string), typeof(CategoryItemCell), string.Empty);

    public string EntryCount
    {
        get => (string)GetValue(EntryCountProperty);
        set => SetValue(EntryCountProperty, value);
    }

    // 2. LEADING ICON (Left side)
    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph), typeof(string), typeof(CategoryItemCell), string.Empty);

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    // 3. ACTION ICON (Button on right side)
    public static readonly BindableProperty ActionIconGlyphProperty = BindableProperty.Create(
        nameof(ActionIconGlyph), typeof(string), typeof(CategoryItemCell), string.Empty);

    public string ActionIconGlyph
    {
        get => (string)GetValue(ActionIconGlyphProperty);
        set => SetValue(ActionIconGlyphProperty, value);
    }

    // 4. COMMAND (Action to run)
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(CategoryItemCell));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    // 5. COMMAND PARAMETER (Which item was clicked?)
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(CategoryItemCell));

    public object CommandParameter
    {
        get => (object)GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}