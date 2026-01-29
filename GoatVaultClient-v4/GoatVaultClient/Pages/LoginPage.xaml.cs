using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /*private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";
        
        // Validation
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Email and password are required.", "OK");
            return;
        }

        try
        {
            var initPayload = new InitRequest { Email = email };

            var initResponse = await _httpService.PostAsync<InitResponse>(
                "http://127.0.0.1:8000/v1/auth/init",
                initPayload
            );

            //Generate local salt 
            string loginVerifier = _userService.GenerateAuthVerifier(password, initResponse.AuthSalt);

            var verifyPayload = new VerifyRequest
            {
                UserId = Guid.Parse(initResponse.UserId),
                AuthVerifier = loginVerifier
            };

            var verifyResponse = await _httpService.PostAsync<VerifyResponse>(
                "http://127.0.0.1:8000/v1/auth/verify",
                verifyPayload
            );

            _authTokenService.SetToken(verifyResponse.AccessToken);

            // Navigate to gratitude page (temporary
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
    }*/

    /*private async void OnGoToRegister(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }*/
}