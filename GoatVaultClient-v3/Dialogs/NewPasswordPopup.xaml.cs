using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient_v3.Pages;

public partial class NewPasswordPopup : PopupPage
{
    public NewPasswordPopup()
    {
        InitializeComponent();
        BindingContext = new ViewModels.NewPasswordPopupViewModel();
    }
}
