using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class OnboardingPage
{
    public OnboardingPage(OnboardingPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}