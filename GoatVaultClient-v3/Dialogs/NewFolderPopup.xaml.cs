using Mopups.Services;
using Mopups.Pages;

namespace GoatVaultClient_v3.Dialogs;

public partial class NewFolderPopup : PopupPage
{
    public NewFolderPopup()
    {
        InitializeComponent();
        BindingContext = new ViewModels.NewFolderPopupViewModel();
    }
}