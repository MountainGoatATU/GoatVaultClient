using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;

namespace GoatVaultClient.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email;
        [ObservableProperty] private string masterPassword;

        // Services
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultSessionService _vaultSessionService;
        private readonly VaultService _vaultService;

        // Constructor
        public UserPageViewModel(
            HttpService httpService,
            AuthTokenService authTokenService,
            VaultSessionService vaultSessionService,
            VaultService vaultService)
        {
            _httpService = httpService;
            _authTokenService = authTokenService;
            _vaultSessionService = vaultSessionService;
            _vaultService = vaultService;

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
            var user = _vaultSessionService.CurrentUser;
            if (user == null)
                return;

            // Ask for current password
            var enteredPassword = await PromptUserAsync("Confirm Password", true);
            if (enteredPassword == null) return;

            // Verify with server
            var authorized = await AuthorizeAsync(enteredPassword);
            if (!authorized)
            {
                return;
            }

            // Ask for new email
            var newEmail = await PromptUserAsync("Enter new email", false);
            if (string.IsNullOrWhiteSpace(newEmail) || newEmail == user.Email) return;

            var oldEmail = user.Email;
            
            // Update local database
            var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
            if (dbUser != null)
            {
                dbUser.Email = newEmail;
                await _vaultService.UpdateUserInLocalAsync(dbUser);
            }

            // Update Server
            try
            {
                var request = new UserRequest
                {
                    Email = newEmail,
                    MfaEnabled = user.MfaEnabled,
                    Vault = user.Vault
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{user.Id}",
                    request
                );

                // Update session a UI
                _vaultSessionService.CurrentUser.Email = updatedUser.Email;
                Email = updatedUser.Email;

                await Shell.Current.DisplayAlert("Success", "Email updated successfully.", "OK");
            }
            catch (Exception ex)
            {
                // Revert local change on failure
                if (dbUser != null)
                {
                    dbUser.Email = oldEmail;
                    await _vaultService.UpdateUserInLocalAsync(dbUser);
                }

                await Shell.Current.DisplayAlert("Error", $"Failed to update email: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            // Not implemented yet
        }

        // Method to show popup and get user input
        private async Task<string?> PromptUserAsync(string title, bool isPassword)
        {
            var popup = new AuthorizePopup(title, isPassword: isPassword);
            
            await MopupService.Instance.PushAsync(popup);
            var result = await popup.WaitForScan();

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        private async Task<bool> AuthorizeAsync(string? enteredPassword = null)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                // Prompt password if not provided
                enteredPassword = await PromptUserAsync("Authorization", true);
                if (string.IsNullOrWhiteSpace(enteredPassword))
                    return false;
            }

            try
            {
                // Same flow as login
                var initPayload = new AuthInitRequest { Email = Email };
                var initResponse = await _httpService.PostAsync<AuthInitResponse>(
                    "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/auth/init",
                    initPayload
                );

                var verifier = CryptoService.GenerateAuthVerifier(enteredPassword, initResponse.AuthSalt);

                var verifyPayload = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(initResponse.UserId),
                    AuthVerifier = verifier
                };

                var verifyResponse = await _httpService.PostAsync<AuthVerifyResponse>(
                    "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/auth/verify",
                    verifyPayload
                );

                // Save the new token and password ?
                _authTokenService.SetToken(verifyResponse.AccessToken);
                MasterPassword = enteredPassword;

                return true;
            }
            catch
            {
                await MopupService.Instance.PushAsync(new IncorrectPasswordPopup());
                return false;
            }
        }
    }
}