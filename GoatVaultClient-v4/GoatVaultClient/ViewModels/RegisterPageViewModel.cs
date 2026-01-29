using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Models;
using GoatVaultClient.Models.API;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;

namespace GoatVaultClient.ViewModels;

public partial class RegisterPageViewModel(
    UserService userService,
    HttpService httpService,
    AuthTokenService authTokenService,
    VaultService vaultService,
    VaultSessionService vaultSessionService)
    : BaseViewModel
{
    // Services

    // Observable Properties (Bound to Entry fields)
    [ObservableProperty] private string email;
    [ObservableProperty] private string password;
    [ObservableProperty] private string confirmPassword;

    // Constructor (Clean Dependency Injection)

    [RelayCommand]
    private async Task Register()
    {
        const string url = "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com";

        if (IsBusy)
            return;

        // 1. Validation
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return;
        }

        try
        {
            IsBusy = true; // Locks UI if you bound ActivityIndicator or buttons

            // 2. Prepare Registration Data
            var registerRequest = userService.RegisterUser(Email, Password, null);

            // Encrypt vault (Initial empty vault)
            var vaultPayload = vaultService.EncryptVault(Password, null);
            registerRequest.Vault = vaultPayload;

            // 3. API: Register
            // Note: Ideally move these URL strings to a Constants file
            var registerResponse = await httpService.PostAsync<AuthRegisterResponse>(
                $"{url}/v1/auth/register",
                registerRequest
            );

            // 4. API: Verify (Get Token)
            var verifyRequest = new AuthVerifyRequest
            {
                UserId = Guid.Parse(registerResponse.Id),
                AuthVerifier = registerRequest.AuthVerifier
            };

            var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                $"{url}/v1/auth/verify",
                verifyRequest
            );

            authTokenService.SetToken(verifyResponse.AccessToken);
            vaultSessionService.MasterPassword = Password;

            // 5. API: Get User Profile
            var userResponse = await httpService.GetAsync<UserResponse>(
                $"{url}/v1/users/{registerResponse.Id}"
            );

            // Update Singleton User Service
            vaultSessionService.CurrentUser = userResponse;

            // 6. Local Database Logic
            // Check if user exists locally, if so, remove them (fresh start)
            var existingUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

            if (existingUser != null)
                await vaultService.DeleteUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

            // Save new user to SQLite
            await vaultService.SaveUserToLocalAsync(new DbModel
            {
                Id = vaultSessionService.CurrentUser.Id,
                Email = vaultSessionService.CurrentUser.Email,
                AuthSalt = vaultSessionService.CurrentUser.AuthSalt,
                MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
                Vault = vaultSessionService.CurrentUser.Vault
            });

            // 7. Decrypt & Store Session in RAM
            vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, Password);

            // 8. Navigate
            // Using Shell navigation is standard for MAUI
            await Shell.Current.GoToAsync(nameof(GratitudePage));

            // If not using Shell routes, use: 
            // await Application.Current.MainPage.Navigation.PushAsync(new GratitudePage(...));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            await Shell.Current.DisplayAlertAsync("Error", "This email is already registered.", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.GoToAsync($"//{nameof(IntroductionPage)}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static async Task GoToLogin()
    {
        // Navigate back to Login
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}