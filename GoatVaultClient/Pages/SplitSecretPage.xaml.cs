using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SplitSecretPage
{
    public SplitSecretPage(SplitSecretViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}