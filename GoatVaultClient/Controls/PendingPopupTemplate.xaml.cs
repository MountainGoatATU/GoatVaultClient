namespace GoatVaultClient.Controls;

public partial class PendingPopupTemplate
{
    public PendingPopupTemplate() => InitializeComponent();

    public static readonly BindableProperty FrameWidthProperty = BindableProperty.Create(
        nameof(FrameWidth),
        typeof(double),
        typeof(PendingPopupTemplate),
        350d); // Set your default width here (350)

    public double FrameWidth
    {
        get => (double)GetValue(FrameWidthProperty);
        set => SetValue(FrameWidthProperty, value);
    }

    public static readonly BindableProperty FrameHeightProperty = BindableProperty.Create(
        nameof(FrameHeight),
        typeof(double),
        typeof(PendingPopupTemplate),
        350d); // Set your default width here (350)

    public double FrameHeight
    {
        get => (double)GetValue(FrameHeightProperty);
        set => SetValue(FrameHeightProperty, value);
    }

    // TITLE
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(PendingPopupTemplate), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}