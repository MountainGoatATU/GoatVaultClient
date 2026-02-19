using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SecurityPage : ContentPage
{
    public SecurityPage(SecurityPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}