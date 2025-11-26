using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;

namespace GoatVaultClient_v3;

public partial class LoginPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly UserService _userService;
    private readonly HttpService _httpService;
    private readonly AuthTokenService _authTokenService;
    private readonly VaultService _vaultService;
    private readonly VaultSessionService _vaultSessionService;

    public LoginPage(IServiceProvider services, UserService userService, HttpService httpService, AuthTokenService authTokenService, VaultService vaultService, VaultSessionService vaultSessionService)
	{
        _services = services;
        _userService = userService;
        _httpService = httpService;
        _authTokenService = authTokenService;
        _vaultService = vaultService;
        _vaultSessionService = vaultSessionService;
        InitializeComponent();
	}
    private async void OnLoginClicked(object sender, EventArgs e)
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

            // Retrieve user
            var userResponse = await _httpService.GetAsync<UserResponse>(
                $"http://127.0.0.1:8000/v1/users/{initResponse.UserId}"
            );

            _userService.User = userResponse;

            // Delete the user if already exists in local storage
            if (await _vaultService.LoadUserFromLocalAsync(_userService.User.Id) != null)
            {
                await _vaultService.DeleteUserFromLocalAsync(_userService.User.Id);
            }

            //Add Vault to local storage
            await _vaultService.SaveUserToLocalAsync( new DbModel
            {
                Id = _userService.User.Id,
                Email = _userService.User.Email,
                AuthSalt = _userService.User.AuthSalt,
                MfaEnabled = _userService.User.MfaEnabled,
                Vault = _userService.User.Vault
            });

            // Decrypt vault
             _vaultSessionService.DecryptedVault = _vaultService.DecryptVault(_userService.User.Vault, password);

            // Navigate to gratitude page (temporary
            var mainPage = _services.GetRequiredService<MainPage>();
            await Navigation.PushAsync(mainPage);
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            await DisplayAlert("Error", "This email is already registered.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

      

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        var registerPage = _services.GetRequiredService<RegisterPage>();
        await Navigation.PushAsync(registerPage);
    }
}