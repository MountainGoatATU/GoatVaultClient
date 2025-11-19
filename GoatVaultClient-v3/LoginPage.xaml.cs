namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Pass a valid IServiceProvider instance to GratitudePage
        await Navigation.PushAsync(new GratitudePage(((App)Application.Current).Services));
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        // Pass a valid IServiceProvider instance to RegisterPage
        await Navigation.PushAsync(new RegisterPage(((App)Application.Current).Services));
    }
}