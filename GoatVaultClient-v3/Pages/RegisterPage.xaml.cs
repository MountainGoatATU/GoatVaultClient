using System;
using GoatVaultClient_v3.Services;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3
{
    public partial class RegisterPage : ContentPage
    {
        private readonly IServiceProvider _services;

        public RegisterPage(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var gratitudePage = _services.GetRequiredService<GratitudePage>();
            await Navigation.PushAsync(gratitudePage);
        }

        private async void OnGoToLogin(object sender, EventArgs e)
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            await Navigation.PushAsync(loginPage);
        }
    }
}
