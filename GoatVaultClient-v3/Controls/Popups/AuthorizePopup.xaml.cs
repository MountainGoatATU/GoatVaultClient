using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient_v3.Controls.Popups;

public partial class AuthorizePopup : PopupPage
{
    public string Result { get; private set; }

    public AuthorizePopup()
	{
		InitializeComponent();
	}

    private async void OnConfirm(object sender, EventArgs e)
    {
        Result = PasswordEntry.Text;
        await MopupService.Instance.PopAsync();
    }

    private async void OnCancel(object sender, EventArgs e)
    {
        Result = null;
        await MopupService.Instance.PopAsync();
    }
}