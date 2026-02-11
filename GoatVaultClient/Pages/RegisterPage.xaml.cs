using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}