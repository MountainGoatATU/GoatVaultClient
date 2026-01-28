using GoatVaultClient_v4.ViewModels;

namespace GoatVaultClient_v4.Pages;

public partial class GratitudePage : ContentPage
{
    public GratitudePage(GratitudePageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}