using System;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3
{
    public partial class RegisterPage : ContentPage
    {
        private readonly IServiceProvider _services;
        private readonly UserService _userService;
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;

        public RegisterPage(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();

            _userService = services.GetRequiredService<UserService>();
            _httpService = services.GetRequiredService<HttpService>();
            _authTokenService = services.GetRequiredService<AuthTokenService>();
            _vaultService = services.GetRequiredService<VaultService>();
            _vaultSessionService = services.GetRequiredService<VaultSessionService>();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? "";
            string password = MasterPasswordEntry.Text ?? "";
            string confirmPassword = ConfirmPasswordEntry.Text ?? "";

            // Validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Email and password are required.", "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            try
            {
                var registerRequest = _userService.RegisterUser(email, password, null);

                // Encrypt vault using the password
                var vaultPayload = _vaultService.EncryptVault(password,null);
                registerRequest.Vault = vaultPayload;

                // Send register request to backend
                var registerResponse = await _httpService.PostAsync<RegisterResponse>(
                    "http://127.0.0.1:8000/v1/auth/register",
                    registerRequest
                );

                //Retrieve the token
                var verifyRequest = new VerifyRequest
                {
                    UserId = Guid.Parse(registerResponse.Id),
                    AuthVerifier = registerRequest.AuthVerifier
                };

                var verifyResponse = await _httpService.PostAsync<VerifyResponse>(
                    "http://127.0.0.1:8000/v1/auth/verify",
                    verifyRequest
                );

                _authTokenService.SetToken(verifyResponse.AccessToken);

                // Retrieve the newly created user
                var userResponse = await _httpService.GetAsync<UserResponse>(
                    $"http://127.0.0.1:8000/v1/users/{registerResponse.Id}"
                );

                // Set the user
                _userService.User = userResponse;

                // Delete the user if already exists in local storage
                if (await _vaultService.LoadUserFromLocalAsync(_userService.User.Id) != null)
                {
                    await _vaultService.DeleteUserFromLocalAsync(_userService.User.Id);
                }

                //Add Vault to local storage
                await _vaultService.SaveUserToLocalAsync(new DbModel
                {
                    Id = _userService.User.Id,
                    Email = _userService.User.Email,
                    AuthSalt = _userService.User.AuthSalt,
                    MfaEnabled = _userService.User.MfaEnabled,
                    Vault = _userService.User.Vault
                });

                // Decrypt vault
                _vaultSessionService.DecryptedVault = _vaultService.DecryptVault(_userService.User.Vault, password);

                // Navigate to next page
                var gratitudePage = _services.GetRequiredService<GratitudePage>();
                await Navigation.PushAsync(gratitudePage);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await DisplayAlert("Error", "This email is already registered.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private async void OnGoToLogin(object sender, EventArgs e)
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            await Navigation.PushAsync(loginPage);
        }
    }
}
