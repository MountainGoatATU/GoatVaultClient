using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient.Controls.Popups;

public partial class IncorrectPasswordPopup : PopupPage
{
	public IncorrectPasswordPopup()
	{
		InitializeComponent();
	}

    private async void OnOkClicked(object sender, EventArgs e)
    {
        try
        {
            await MopupService.Instance.PopAsync();
        }
        catch
        {
            throw; // TODO handle exception
        }
    }
}