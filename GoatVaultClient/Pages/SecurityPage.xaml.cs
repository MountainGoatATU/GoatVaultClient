using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SecurityPage
{
    public SecurityPage(SecurityPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}