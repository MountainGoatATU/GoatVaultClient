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

        public RegisterPage(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();

            _userService = services.GetRequiredService<UserService>();
            _httpService = services.GetRequiredService<HttpService>();
            _authTokenService = services.GetRequiredService<AuthTokenService>();
            _vaultService = services.GetRequiredService<VaultService>();
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
                var vaultPayload = _vaultService.EncryptVault(password, new VaultData());
                registerRequest.Vault = vaultPayload;

                // Send request to backend
                var response = await _httpService.PostAsync<RegisterResponse>(
                    "http://127.0.0.1:8000/v1/auth/register",
                    registerRequest
                );

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
