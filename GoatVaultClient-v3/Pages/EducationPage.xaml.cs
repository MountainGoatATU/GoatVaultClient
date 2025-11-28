using GoatVaultClient_v3.ViewModels;

namespace GoatVaultClient_v3;

public partial class EducationPage : ContentPage
{
    public EducationPage(EducationPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}