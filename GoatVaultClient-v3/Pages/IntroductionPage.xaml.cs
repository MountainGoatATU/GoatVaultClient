using GoatVaultClient_v3.ViewModels;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3;

public partial class IntroductionPage : ContentPage
{
    public IntroductionPage(IntroductionPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}