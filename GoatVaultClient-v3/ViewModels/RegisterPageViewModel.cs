using System.Net.Http.Json; // Ensure you have this for Http extensions if needed
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class RegisterPageViewModel : BaseViewModel
    {
        // Services
        private readonly UserService _userService;
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;

        // Observable Properties (Bound to Entry fields)
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string confirmPassword;

        // Constructor (Clean Dependency Injection)
        public RegisterPageViewModel(
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
        private async Task Register()
        {
            string url = "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com";
            if (IsBusy) return;

            // 1. Validation
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Error", "Email and password are required.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            try
            {
                IsBusy = true; // Locks UI if you bound ActivityIndicator or buttons

                // 2. Prepare Registration Data
                var registerRequest = _userService.RegisterUser(Email, Password, null);

                // Encrypt vault (Initial empty vault)
                var vaultPayload = _vaultService.EncryptVault(Password, null);
                registerRequest.Vault = vaultPayload;

                // 3. API: Register
                // Note: Ideally move these URL strings to a Constants file
                var registerResponse = await _httpService.PostAsync<AuthRegisterResponse>(
                    $"{url}/v1/auth/register",
                    registerRequest
                );

                // 4. API: Verify (Get Token)
                var verifyRequest = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(registerResponse.Id),
                    AuthVerifier = registerRequest.AuthVerifier
                };

                var verifyResponse = await _httpService.PostAsync<AuthVerifyResponse>(
                    $"{url}/v1/auth/verify",
                    verifyRequest
                );

                _authTokenService.SetToken(verifyResponse.AccessToken);
                _vaultSessionService.MasterPassword = Password;

                // 5. API: Get User Profile
                var userResponse = await _httpService.GetAsync<UserResponse>(
                    $"{url}/v1/users/{registerResponse.Id}"
                );

                // Update Singleton User Service
                _vaultSessionService.CurrentUser = userResponse;

                // 6. Local Database Logic
                // Check if user exists locally, if so, remove them (fresh start)
                var existingUser = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);
                if (existingUser != null)
                {
                    await _vaultService.DeleteUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);
                }

                // Save new user to SQLite
                await _vaultService.SaveUserToLocalAsync(new DbModel
                {
                    Id = _vaultSessionService.CurrentUser.Id,
                    Email = _vaultSessionService.CurrentUser.Email,
                    AuthSalt = _vaultSessionService.CurrentUser.AuthSalt,
                    MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled,
                    Vault = _vaultSessionService.CurrentUser.Vault
                });

                // 7. Decrypt & Store Session in RAM
                _vaultSessionService.DecryptedVault = _vaultService.DecryptVault(_vaultSessionService.CurrentUser.Vault, Password);

                // 8. Navigate
                // Using Shell navigation is standard for MAUI
                await Shell.Current.GoToAsync(nameof(GratitudePage));

                // If not using Shell routes, use: 
                // await Application.Current.MainPage.Navigation.PushAsync(new GratitudePage(...));
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await Shell.Current.DisplayAlert("Error", "This email is already registered.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.GoToAsync($"//{nameof(IntroductionPage)}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToLogin()
        {
            // Navigate back to Login
            await Shell.Current.GoToAsync($"{nameof(LoginPage)}");
        }
    }
}