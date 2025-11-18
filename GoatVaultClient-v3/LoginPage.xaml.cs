namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GratitudePage());
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}