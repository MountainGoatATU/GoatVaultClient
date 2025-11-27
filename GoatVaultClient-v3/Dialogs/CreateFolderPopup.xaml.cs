using Mopups.Services;
using Mopups.Pages;

namespace GoatVaultClient_v3.Dialogs;

public partial class CreateFolderPopup : PopupPage
{
	public CreateFolderPopup()
	{
		InitializeComponent();
	}
    private void Cancel_Clicked(object sender, EventArgs e) => MopupService.Instance.PopAsync();

    private void Create_Clicked(object sender, EventArgs e)
    {
        MopupService.Instance.PopAsync();
    }
}