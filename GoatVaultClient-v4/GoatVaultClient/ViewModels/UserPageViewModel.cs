using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using Mopups.Services;

namespace GoatVaultClient.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email = "user@example.com";
        [ObservableProperty] private string masterPassword = "password123";

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            if (!await AuthorizeAsync())
                return;

            // TODO: AuthorizePopup does not have a parameter named 'buttonText'?
            var popup = new AuthorizePopup(isPassword: false/*, buttonText: "Save"*/)
            {
                Title = "Edit Email"
            };

            await MopupService.Instance.PushAsync(popup);
            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            var result = await popup.WaitForScan();

            if (!string.IsNullOrWhiteSpace(result))
                Email = result;
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            if (!await AuthorizeAsync())
            {
                return;
            }

            // TODO: AuthorizePopup does not have a parameter named 'buttonText'?
            var popup = new AuthorizePopup(isPassword: true /*, buttonText: "Save"*/)
            {
                Title = "Save"
            };
            await MopupService.Instance.PushAsync(popup);

            var result = await popup.WaitForScan();

            if (!string.IsNullOrWhiteSpace(result))
                MasterPassword = result;
        }

        private async Task<bool> AuthorizeAsync()
        {
            // TODO: AuthorizePopup does not have a parameter named 'buttonText'?
            var popup = new AuthorizePopup(isPassword: true /*, buttonText: "OK"*/)
            {
                Title = "Authorization"
            };

            await MopupService.Instance.PushAsync(popup);

            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            var result = await popup.WaitForScan();

            if (result == MasterPassword)
                return true;

            var errorPopup = new IncorrectPasswordPopup();
            await MopupService.Instance.PushAsync(errorPopup);
            return false;

        }
    }
}