namespace GoatVaultClient.Controls;

public partial class Component : ContentView
{
	public Component()
	{
		InitializeComponent();

        if (Application.Current != null && Application.Current.Resources.TryGetValue("componentMarginLarge", out var defaultMargin))
        {
            BorderMargin = (Thickness)defaultMargin;
        }
	}

    public static readonly BindableProperty BorderMarginProperty = BindableProperty.Create(
        nameof(BorderMargin),
        typeof(Thickness),
        typeof(Component),
        default(Thickness)); // Defaults to 0

    public Thickness BorderMargin
    {
        get => (Thickness)GetValue(BorderMarginProperty);
        set => SetValue(BorderMarginProperty, value);
    }
}