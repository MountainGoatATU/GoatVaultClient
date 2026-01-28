using GoatVaultClient_v4.ViewModels;

namespace GoatVaultClient_v4.Pages;

public partial class UserPage : ContentPage
{
    public UserPage(UserPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}