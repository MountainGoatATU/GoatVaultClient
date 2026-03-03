using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class SettingsPage
{
    public SettingsPage(SettingsPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}