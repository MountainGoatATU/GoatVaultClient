using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Pages;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;

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
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string? _confirmPassword;

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
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "Email and password are required.",
                aText: "OK"
            ));
            return;
        }

        if (Password != ConfirmPassword)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "Passwords do not match.",
                aText: "OK"
            ));
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
                MfaSecret = vaultSessionService.CurrentUser.MfaSecret,
                Vault = vaultSessionService.CurrentUser.Vault,
                CreatedAt = vaultSessionService.CurrentUser.CreatedAt,
                UpdatedAt = vaultSessionService.CurrentUser.UpdatedAt
            });

            // 7. Decrypt & Store Session in RAM
            vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, Password);

            // 8. Navigate
            await Shell.Current.GoToAsync(nameof(GratitudePage));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Email Already Registered",
                body: "An account with this email already exists. Please use a different email or try logging in.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Invalid Input",
                body: "Please check your email format and password requirements.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx)
        {
            // Other HTTP errors
            System.Diagnostics.Debug.WriteLine($"Registration HTTP error: {httpEx}");
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Registration Failed",
                body: $"Unable to complete registration. {httpEx.Message}",
                aText: "OK"
            ));
        }
        catch (TimeoutException)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Timeout",
                body: "The registration request timed out. Please check your internet connection and try again.",
                aText: "OK"
            ));
        }
        catch (Exception ex)
        {
            // Log the error and show user-friendly message
            System.Diagnostics.Debug.WriteLine($"Registration error: {ex}");
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "An unexpected error occurred during registration. Please try again later.",
                aText: "OK"
            ));
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
