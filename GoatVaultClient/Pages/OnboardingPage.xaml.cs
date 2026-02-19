using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(OnboardingPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}