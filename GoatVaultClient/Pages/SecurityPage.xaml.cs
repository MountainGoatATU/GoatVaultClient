using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SecurityPage
{
    public SecurityPage(SecurityPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SecurityPageViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}