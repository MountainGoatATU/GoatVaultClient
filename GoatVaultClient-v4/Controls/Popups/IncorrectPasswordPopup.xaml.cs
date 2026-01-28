using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient_v4.Controls.Popups;

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