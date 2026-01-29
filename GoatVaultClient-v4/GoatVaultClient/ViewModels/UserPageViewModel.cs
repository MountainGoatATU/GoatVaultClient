using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using Mopups.Services;

namespace GoatVaultClient.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email = "user@example.com";
        [ObservableProperty]private string masterPassword = "password123";

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            // Authorize request
            if (!await AuthorizeAsync())
            {
                return;
            }
            // Create a AuthorizePopup object
            var popup = new AuthorizePopup(isPassword: false, buttonText: "Save");
            // Pushing AuthorizePopup to the MopupService
            await MopupService.Instance.PushAsync(popup);
            // Awaiting Result from the popup
            string? result = await popup.WaitForScan();
            // If result is not null or whitespace, update the Email property
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

            var popup = new AuthorizePopup(isPassword: true, buttonText: "Save");
            await MopupService.Instance.PushAsync(popup);
            
            string? result = await popup.WaitForScan();

            if (!string.IsNullOrWhiteSpace(result))
                MasterPassword = result;
        }

        private async Task<bool> AuthorizeAsync()
        {
            // Create a AuthorizePopup object
            var popup = new AuthorizePopup(isPassword: true, buttonText: "OK");
            // Pushing AuthorizePopup to the MopupService
            await MopupService.Instance.PushAsync(popup);
            // Awaiting Result from the popup
            string? result = await popup.WaitForScan();
            // If result is null, return false
            if (result == null)
                return false;
            // If result does not match MasterPassword, show IncorrectPasswordPopup and return false
            if (result != MasterPassword)
            {
                await MopupService.Instance.PopAllAsync();
                var errorPopup = new IncorrectPasswordPopup();
                await MopupService.Instance.PushAsync(errorPopup);
                return false;
            }

            return true;
        }
    }
}
