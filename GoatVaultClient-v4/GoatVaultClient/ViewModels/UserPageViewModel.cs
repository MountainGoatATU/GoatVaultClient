using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.API;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;

namespace GoatVaultClient.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email = "";
        [ObservableProperty] private string masterPassword = "";

        // Services
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultSessionService _vaultSessionService;

        // Constructor
        public UserPageViewModel(
            HttpService httpService,
            AuthTokenService authTokenService,
            VaultSessionService vaultSessionService)
        {
            _httpService = httpService;
            _authTokenService = authTokenService;
            _vaultSessionService = vaultSessionService;

            // Initialize properties from current session
            if (_vaultSessionService.CurrentUser != null)
            {
                Email = _vaultSessionService.CurrentUser.Email;
                MasterPassword = _vaultSessionService.MasterPassword;
            }
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            if (!await AuthorizeAsync())
                return;

            var popup = new AuthorizePopup(isPassword: false)
            {
                Title = "Edit Email"
            };
            
            await MopupService.Instance.PushAsync(popup);
            while (MopupService.Instance.PopupStack.Contains(popup))
                await Task.Delay(50);

            var newEmail = await popup.WaitForScan();

            // If the user cancelled or provided an empty email, do nothing
            if (string.IsNullOrWhiteSpace(newEmail)) return;

            var payload = new UserUpdateRequest { Email = newEmail };
            var userId = _vaultSessionService.CurrentUser.Id;

            // Send PATCH request to update email
            var updatedUser = await _httpService.PatchAsync<UserResponse>(
                $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{userId}",
                payload
            );

            Email = updatedUser.Email;
            _vaultSessionService.CurrentUser = updatedUser;
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            if (!await AuthorizeAsync())
            {
                return;
            }

            var popup = new AuthorizePopup(isPassword: true)
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