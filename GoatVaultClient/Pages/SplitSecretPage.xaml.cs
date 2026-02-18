using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SplitSecretPage : ContentPage
{
	public SplitSecretPage(SplitSecretViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}