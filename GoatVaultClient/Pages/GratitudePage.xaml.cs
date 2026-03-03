using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class GratitudePage
{
    public GratitudePage(GratitudePageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}