using GoatVaultClient.Pages;

namespace GoatVaultClient;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for pages that use relative navigation (not declared in Shell XAML)
        Routing.RegisterRoute(nameof(SplitSecretPage), typeof(SplitSecretPage));
        Routing.RegisterRoute(nameof(RecoverSecretPage), typeof(RecoverSecretPage));
        Routing.RegisterRoute(nameof(EducationDetailPage), typeof(EducationDetailPage));
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