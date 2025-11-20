using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;

namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly HttpService _httpService;
    private readonly UserService _userService;

    public LoginPage(IServiceProvider services)
	{
        _services = services;
        _httpService = services.GetRequiredService<HttpService>();
        _userService = services.GetRequiredService<UserService>();
        InitializeComponent();
	}
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = MasterPasswordEntry.Text ?? "";

        // Validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Email and password are required.", "OK");
            return;
        }

        try
        {
            // Get user data from server
            string getUserUrl = $"http://127.0.0.1:8000/v1/users/{email}";
            var storedUser = await _httpService.GetAsync<UserPayload>(getUserUrl);

            if (storedUser == null)
            {
                await DisplayAlert("Error", "User not found.", "OK");
                return;
            }

            // Verify login
            var status = _userService.LoginUser(email, password, storedUser.Salt, storedUser.Password_hash);

            if (status == LoginStatus.Success)
            {
                var mainPage = _services.GetRequiredService<MainPage>();
                await Navigation.PushAsync(mainPage);
            }
            else
            {
                await DisplayAlert("Login Failed", "Email or password incorrect.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}