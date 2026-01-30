using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class IntroductionPage : ContentPage
{
    public IntroductionPage(IntroductionPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}