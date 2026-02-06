using GoatVaultClient.Pages;

namespace GoatVaultClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    public void EnableFlyout()
    {
        FlyoutBehavior = FlyoutBehavior.Flyout;
    }
    public void DisableFlyout()
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
    }

}