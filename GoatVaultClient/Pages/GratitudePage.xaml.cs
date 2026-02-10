using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class GratitudePage : ContentPage
{
    public GratitudePage(GratitudePageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}