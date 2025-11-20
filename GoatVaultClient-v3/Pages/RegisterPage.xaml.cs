using System;
using GoatVaultClient_v3.Services;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3
{
    public partial class RegisterPage : ContentPage
    {
        private readonly IServiceProvider _services;
        private readonly HttpService _httpService;
        private readonly UserService _userService;

        public RegisterPage(IServiceProvider services)
        {
            _services = services;
            _httpService = services.GetRequiredService<HttpService>();
            _userService = services.GetRequiredService<UserService>();
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string email = UsernameEntry.Text?.Trim();
            string password = MasterPasswordEntry.Text;
            string confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
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
                var registerPayload = new
                {
                    email = email,
                    password = password
                };

                var response = await _httpService.PostAsync<object>("/v1/users/", registerPayload);

                var gratitudePage = _services.GetRequiredService<GratitudePage>();
                await Navigation.PushAsync(gratitudePage);
            }
            catch (HttpRequestException httpEx)
            {
                await DisplayAlert("HTTP Error", httpEx.Message, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
            }
        }

        private async void OnGoToLogin(object sender, EventArgs e)
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            await Navigation.PushAsync(loginPage);
        }
    }
}
