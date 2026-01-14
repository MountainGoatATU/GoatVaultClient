using System.Reflection.PortableExecutable;

namespace GoatVaultClient_v3.Controls;

public partial class ComponentHeader : ContentView
{
	public ComponentHeader()
	{
		InitializeComponent();
	}

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ComponentHeader), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(
        nameof(Actions), typeof(View), typeof(ComponentHeader));

    public View Actions
    {
        get => (View)GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}