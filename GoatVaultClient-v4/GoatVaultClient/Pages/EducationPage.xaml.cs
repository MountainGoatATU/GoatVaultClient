using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class EducationPage : ContentPage
{
    public EducationPage(EducationPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}