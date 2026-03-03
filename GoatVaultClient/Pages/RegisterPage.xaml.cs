using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class RegisterPage
{
    public RegisterPage(RegisterPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}