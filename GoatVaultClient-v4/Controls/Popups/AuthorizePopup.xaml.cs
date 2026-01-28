using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient_v4.Controls.Popups;

public partial class AuthorizePopup : PopupPage
{
    public string Result { get; private set; }

    public AuthorizePopup(string title = "Authorize", bool isPassword = true, string buttonText = "OK")
	{
		InitializeComponent();

        TitleLabel.Text = title;
        InputEntry.IsPassword = isPassword;
        SaveButton.Text = buttonText;
        ShowHideAttachment.IsVisible = isPassword;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        Result = InputEntry.Text;
        await MopupService.Instance.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        Result = null;
        await MopupService.Instance.PopAsync();
    }
}