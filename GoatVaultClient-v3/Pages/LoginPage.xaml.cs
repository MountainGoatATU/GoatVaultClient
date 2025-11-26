using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}