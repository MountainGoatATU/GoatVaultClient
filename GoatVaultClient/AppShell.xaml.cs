using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;

namespace GoatVaultClient;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        // Register routes for pages that use relative navigation (not declared in Shell XAML)
        Routing.RegisterRoute(nameof(SplitSecretPage), typeof(SplitSecretPage));
        Routing.RegisterRoute(nameof(RecoverSecretPage), typeof(RecoverSecretPage));
        Routing.RegisterRoute(nameof(EntryDetailPage), typeof(Pages.EntryDetailPage));
    }

    public void EnableFlyout() => FlyoutBehavior = FlyoutBehavior.Flyout;

    public void DisableFlyout() => FlyoutBehavior = FlyoutBehavior.Disabled;

}