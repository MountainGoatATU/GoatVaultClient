using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class UserPage : ContentPage
{
    public UserPage(UserPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}