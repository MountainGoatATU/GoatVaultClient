using System.Windows.Input;

namespace GoatVaultClient.Controls;

public partial class PopupTemplate : ContentView
{
    public PopupTemplate()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty FrameWidthProperty = BindableProperty.Create(
        nameof(FrameWidth),
        typeof(double),
        typeof(PopupTemplate),
        350d); // Set your default width here (350)

    public double FrameWidth
    {
        get => (double)GetValue(FrameWidthProperty);
        set => SetValue(FrameWidthProperty, value);
    }

    public static readonly BindableProperty FrameHeightProperty = BindableProperty.Create(
        nameof(FrameHeight),
        typeof(double),
        typeof(PopupTemplate),
        350d); // Set your default width here (350)

    public double FrameHeight
    {
        get => (double)GetValue(FrameHeightProperty);
        set => SetValue(FrameHeightProperty, value);
    }

    // TITLE
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(PopupTemplate), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // COMMANDS
    public static readonly BindableProperty AcceptCommandProperty = BindableProperty.Create(
        nameof(AcceptCommand), typeof(ICommand), typeof(PopupTemplate));

    public ICommand AcceptCommand
    {
        get => (ICommand)GetValue(AcceptCommandProperty);
        set => SetValue(AcceptCommandProperty, value);
    }

    public static readonly BindableProperty CancelCommandProperty = BindableProperty.Create(
        nameof(CancelCommand), typeof(ICommand), typeof(PopupTemplate));

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    // BUTTON TEXT (Optional, with defaults)
    public static readonly BindableProperty AcceptTextProperty = BindableProperty.Create(
        nameof(AcceptText), typeof(string), typeof(PopupTemplate), "Save");

    public string AcceptText
    {
        get => (string)GetValue(AcceptTextProperty);
        set => SetValue(AcceptTextProperty, value);
    }

    public static readonly BindableProperty CancelTextProperty = BindableProperty.Create(
        nameof(CancelText), typeof(string), typeof(PopupTemplate), "Cancel");

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }
}