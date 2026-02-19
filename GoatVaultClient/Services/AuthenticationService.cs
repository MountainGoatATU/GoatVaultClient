using GoatVaultApplication.Auth;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Microsoft.Extensions.Logging;
using Mopups.Services;
using Email = GoatVaultCore.Models.Objects.Email;

// TODO: REFACTOR only hold use cases
namespace GoatVaultClient.Services;

public interface IAuthenticationService
{
    public Task<bool> LoginAsync(string? email, string? password, Func<Task<string?>> mfaCodeProvider);
    public Task<bool> LoginOfflineAsync(string? email, string? password, User? selectedUser);
    public Task<bool> RegisterAsync(string email, string? password, string? confirmPassword);
    public Task LogoutAsync();
    public Task<List<User>> GetAllLocalAccountsAsync();
    public Task RemoveLocalAccountAsync(User account);
}
public class AuthenticationService(
    LoginOnlineUseCase loginOnline,
    LoginOfflineUseCase loginOffline,
    RegisterUseCase register,
    LogoutUseCase logout,
    IUserRepository users,
    ConnectivityService connectivity,
    ILogger<AuthenticationService>? logger = null
    ) : IAuthenticationService
{
    public async Task<bool> LoginAsync(string? email, string? password, Func<Task<string?>> mfaCodeProvider)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
            return false;
        }

        try
        {
            logger?.LogInformation("Login attempt initiated.");

            // Check connectivity before network call
            var hasConnection = connectivity.CheckConnectivity();
            if (!hasConnection)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    "Unable to verify internet connection.",
                    "OK");
                return false;
            }

            await loginOnline.ExecuteAsync(new Email(email!), password!, mfaCodeProvider);

            return true;
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger?.LogWarning("Login failed: unauthorized");
            await Shell.Current.DisplayAlertAsync("Login Failed", "Invalid email or password (or MFA code). Please try again.", "OK");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger?.LogWarning("Login failed: account not found");
            await Shell.Current.DisplayAlertAsync("Account Not Found", "No account found with this email address.", "OK");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            logger?.LogWarning("Login failed: bad request");
            await Shell.Current.DisplayAlertAsync("Invalid Request", "Please check your email and password.", "OK");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger?.LogWarning("Login failed: conflict");
            await Shell.Current.DisplayAlertAsync("Error", "This email is already registered.", "OK");
        }
        catch (HttpRequestException httpEx)
        {
            logger?.LogError(httpEx, "Login connection error");
            // Network-related errors
            await Shell.Current.DisplayAlertAsync(
                "Connection Error",
                $"Unable to connect to the server. {httpEx.Message}",
                "OK");
        }
        catch (TaskCanceledException)
        {
            logger?.LogWarning("Login timed out");
            // Timeout errors
            await Shell.Current.DisplayAlertAsync(
                "Timeout",
                "The request timed out. Please try again.",
                "OK");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("decrypt"))
        {
            logger?.LogError(ex, "Vault decryption failed during login");
            await Shell.Current.DisplayAlertAsync(
                "Decryption Error",
                "Unable to decrypt your vault. This may indicate a data corruption issue.",
                "OK");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error during login");
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }

        return false;
    }
    public async Task<bool> LoginOfflineAsync(string? email, string? password, User? selectedUser)
    {
        // Validation
        if (selectedUser == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please select an account.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Password is required.", "OK");
            return false;
        }

        try
        {
            await loginOffline.ExecuteAsync(selectedUser.Id, password!);
            logger?.LogInformation("Offline login attempt for account {AccountId}", selectedUser.Id);
        }
        catch (Exception ex)
        {
            // Decryption failed - wrong password
            logger?.LogWarning(ex, "Offline login failed for account {AccountId} — likely wrong password", selectedUser.Id);
            await Shell.Current.DisplayAlertAsync(
                "Error",
                "Incorrect password for this account.",
                "OK");
            return false;
        }

        return true;
    }
    public async Task<bool> RegisterAsync(string email, string? password, string? confirmPassword)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
            return false;
        }

        // Password Match
        if (password != confirmPassword)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
            return false;
        }

        try
        {
            logger?.LogInformation("Registration attempt.");

            // Check connectivity before network call
            var hasConnection = connectivity.CheckConnectivity();
            if (!hasConnection)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    "Unable to verify internet connection.",
                    "OK");
                return false;
            }

            await register.ExecuteAsync(new Email(email), password!);

            logger?.LogInformation("Registration successful");
            return true;
        }
        catch (HttpRequestException)
        {
            logger?.LogError("Registration connection error.");
            // Network-related errors
            await Shell.Current.DisplayAlertAsync(
                "Connection Error",
                "Unable to connect to the server. Please check your internet connection.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            logger?.LogWarning("Registration timed out.");
            // Timeout errors
            await Shell.Current.DisplayAlertAsync(
                "Timeout",
                "The request timed out. Please try again.",
                "OK");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error during registration.");
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }

        return false;
    }
    public async Task LogoutAsync()
    {
        try
        {
            logger?.LogInformation("Logout initiated");

            // Confirm logout action
            var confirmPopup = new PromptPopup(
                popupTitle: "Logout",
                body: "Are you sure you want to logout? All changes will be saved.",
                aText: "Logout",
                cText: "Cancel"
            );
            await MopupService.Instance.PushAsync(confirmPopup);
            var confirm = await confirmPopup.WaitForScan();
            await MopupService.Instance.PopAsync();

            // User cancelled logout
            if (!confirm)
                return;

            // Show pending popup while we process logout
            await MopupService.Instance.PushAsync(new PendingPopup("Logging out..."));

            await logout.ExecuteAsync();

            // Pop the pending popup
            await MopupService.Instance.PopAsync();

            // Disable flyout menu to prevent navigation during logout
            ((AppShell)Shell.Current).DisableFlyout();

            // Navigate to login page
            await Shell.Current.GoToAsync("//login");

            logger?.LogInformation("Logout completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Logout failed");
            await MopupService.Instance.PushAsync(new PromptPopup(
                popupTitle: "Error",
                body: "Failed to logout properly. Please try again.",
                aText: "OK"
            ));
        }
    }
    public async Task<List<User>> GetAllLocalAccountsAsync() => await users.GetAllAsync();
    public async Task RemoveLocalAccountAsync(User account)
    {
        // Confirm deletion
        var confirmPopup = new PromptPopup(
                popupTitle: "Remove Local Account",
                body: "Are you sure you want to delete this account from local storage? All changes will be saved.",
                aText: "Delete",
                cText: "Cancel"
            );
        await MopupService.Instance.PushAsync(confirmPopup);
        var confirm = await confirmPopup.WaitForScan();

        if (!confirm)
            return;

        await users.DeleteAsync(account);
    }
}
