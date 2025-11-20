using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;

namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
    private readonly IServiceProvider _services;

    public LoginPage(IServiceProvider services)
	{
        _services = services;
        InitializeComponent();
	}
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var mainPage = _services.GetRequiredService<MainPage>();
        await Navigation.PushAsync(mainPage);
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}