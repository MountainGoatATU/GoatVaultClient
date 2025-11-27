using Mopups.Services;
using Mopups.Pages;

namespace GoatVaultClient_v3.Dialogs;

public partial class NewFolderPopup : PopupPage
{
	public NewFolderPopup()
	{
		InitializeComponent();
	}
    private void Cancel_Clicked(object sender, EventArgs e) => MopupService.Instance.PopAsync();

    private void Create_Clicked(object sender, EventArgs e)
    {
        MopupService.Instance.PopAsync();
    }
}