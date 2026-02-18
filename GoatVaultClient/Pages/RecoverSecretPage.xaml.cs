using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class RecoverSecretPage : ContentPage
{
	public RecoverSecretPage(RecoverSecretViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}