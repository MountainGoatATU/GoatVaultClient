using GoatVaultClient_v4.ViewModels;

namespace GoatVaultClient_v4.Pages;

public partial class EducationPage : ContentPage
{
    public EducationPage(EducationPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}