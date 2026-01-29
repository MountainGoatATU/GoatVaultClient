using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using Mopups.Services;

namespace GoatVaultClient.ViewModels;

public partial class UserPageViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _email = "user@example.com";

    [ObservableProperty]
    private string _masterPassword = "password123";

    [RelayCommand]
    private async Task EditEmailAsync()
    {
        if (!await AuthorizeAsync())
            return;

        var popup = new AuthorizePopup(title: "Edit Email", isPassword: false, buttonText: "Save");
        await MopupService.Instance.PushAsync(popup);
        while (MopupService.Instance.PopupStack.Contains(popup))
            await Task.Delay(50);

        if (!string.IsNullOrWhiteSpace(popup.Result))
            Email = popup.Result;
    }

    [RelayCommand]
    private async Task EditMasterPasswordAsync()
    {
        if (!await AuthorizeAsync())
            return;

        var popup = new AuthorizePopup(title: "Edit Master Password", isPassword: true, buttonText: "Save");
        await MopupService.Instance.PushAsync(popup);

        while (MopupService.Instance.PopupStack.Contains(popup))
            await Task.Delay(50);

        if (!string.IsNullOrWhiteSpace(popup.Result))
            MasterPassword = popup.Result;
    }

    private async Task<bool> AuthorizeAsync()
    {
        var popup = new AuthorizePopup(title: "Authorization", isPassword: true, buttonText: "OK");
        await MopupService.Instance.PushAsync(popup);

        while (MopupService.Instance.PopupStack.Contains(popup))
            await Task.Delay(50);

        if (popup.Result == MasterPassword)
            return true;

        var errorPopup = new IncorrectPasswordPopup();
        await MopupService.Instance.PushAsync(errorPopup);
        return false;
    }
}