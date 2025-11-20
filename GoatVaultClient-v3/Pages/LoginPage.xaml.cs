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
        var gratitudePage = _services.GetRequiredService<GratitudePage>();
        await Navigation.PushAsync(gratitudePage);
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}