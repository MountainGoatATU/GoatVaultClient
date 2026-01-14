using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using static System.Net.WebRequestMethods;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        // Dependencies
        private readonly UserService _userService;
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;

        // Observable Properties
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        public LoginPageViewModel(
            UserService userService,
            HttpService httpService,
            AuthTokenService authTokenService,
            VaultService vaultService,
            VaultSessionService vaultSessionService)
        {
            _userService = userService;
            _httpService = httpService;
            _authTokenService = authTokenService;
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
        }

        [RelayCommand]
        private async Task Login()
        {
            string url = "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com";
            if (IsBusy) return;

            // 1. Validation
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Error", "Email and password are required.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // 2. Init Auth
                var initPayload = new AuthInitRequest { Email = Email };
                var initResponse = await _httpService.PostAsync<AuthInitResponse>(
                    $"{url}/v1/auth/init",
                    initPayload
                );

                // 3. Generate Verifier
                string loginVerifier = _userService.GenerateAuthVerifier(Password, initResponse.AuthSalt);

                // 4. Verify
                var verifyPayload = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(initResponse.UserId),
                    AuthVerifier = loginVerifier
                };
                var verifyResponse = await _httpService.PostAsync<AuthVerifyResponse>(
                    $"{url}/v1/auth/verify",
                    verifyPayload
                );

                _authTokenService.SetToken(verifyResponse.AccessToken);
                _vaultSessionService.MasterPassword = Password;

                // 5. Get User Data
                var userResponse = await _httpService.GetAsync<UserResponse>(
                    $"{url}/v1/users/{initResponse.UserId}"
                );

                _vaultSessionService.CurrentUser = userResponse;

                // 6. Sync Local DB (Delete old if exists, save new)
                var existingUser = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);
                if (existingUser != null)
                {
                    await _vaultService.DeleteUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);
                }

                await _vaultService.SaveUserToLocalAsync(new DbModel
                {
                    Id = _vaultSessionService.CurrentUser.Id,
                    Email = _vaultSessionService.CurrentUser.Email,
                    AuthSalt = _vaultSessionService.CurrentUser.AuthSalt,
                    MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled,
                    Vault = _vaultSessionService.CurrentUser.Vault
                });

                // 7. Decrypt & Store Session
                _vaultSessionService.DecryptedVault = _vaultService.DecryptVault(_vaultSessionService.CurrentUser.Vault, Password);

                // 8. Navigate to App (MainPage)
                // Note: Originally you went to GratitudePage, but for login, MainPage is standard.
                // Using "//MainPage" clears the stack so 'Back' doesn't go to Login.
                Application.Current.MainPage = new AppShell();
                await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await Shell.Current.DisplayAlert("Error", "This email is already registered.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToRegister()
        {
            await Shell.Current.GoToAsync($"//{nameof(RegisterPage)}");
        }
    }
}