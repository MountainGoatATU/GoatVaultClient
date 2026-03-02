using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class RecoverSecretPage
{
    public RecoverSecretPage(RecoverSecretViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}