namespace GoatVaultClient_v3;

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
        await Navigation.PushAsync(new GratitudePage(_services));
    }

    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }
}