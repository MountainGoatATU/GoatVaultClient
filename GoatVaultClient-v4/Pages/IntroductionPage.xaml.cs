using GoatVaultClient_v4.ViewModels;

namespace GoatVaultClient_v4.Pages;

public partial class IntroductionPage : ContentPage
{
    public IntroductionPage(IntroductionPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}