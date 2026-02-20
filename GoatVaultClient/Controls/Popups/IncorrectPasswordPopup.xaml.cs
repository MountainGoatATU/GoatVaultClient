using Microsoft.Extensions.Logging;
using Mopups.Pages;
using Mopups.Services;

namespace GoatVaultClient.Controls.Popups;

public partial class IncorrectPasswordPopup : PopupPage
{
    private readonly ILogger<IncorrectPasswordPopup>? _logger;
    public IncorrectPasswordPopup(ILogger<IncorrectPasswordPopup>? logger = null)
    {
        _logger = logger;
        InitializeComponent();
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        try
        {
            await MopupService.Instance.PopAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during OnOkClicked() of IncorrectPasswordPopup");
        }
    }
}