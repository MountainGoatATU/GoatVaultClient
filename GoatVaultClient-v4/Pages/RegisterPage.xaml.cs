using GoatVaultClient_v4.ViewModels;

namespace GoatVaultClient_v4.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}