namespace GoatVaultClient_v3;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new GratitudePage());
    }

    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }
}