using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient_v3.Controls.Popups;

public partial class IncorrectPasswordPopup : PopupPage
{
	public IncorrectPasswordPopup()
	{
		InitializeComponent();
	}

    private async void OnOkClicked(object sender, EventArgs e)
    {
        await MopupService.Instance.PopAsync();
    }
}