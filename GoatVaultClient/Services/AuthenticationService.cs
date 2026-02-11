using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Services;

namespace GoatVaultClient.Services
{
    public interface IAuthenticationService
    {
        public Task<bool> LoginAsync(string? email, string? password, Func<Task<string?>> mfaCodeProvider);
        public Task<bool> LoginOfflineAsync(string? email, string? password, DbModel? selectedAccount);
        public Task RegisterAsync(string email, string? password, string? confirmPassword);
        public Task LogoutAsync();
        public Task<List<DbModel>> GetAllLocalAccountsAsync();
        public Task RemoveLocalAccountAsync(DbModel account);
    }
    public class AuthenticationService(
        IConfiguration configuration,
        VaultService vaultService,
        AuthTokenService authTokenService,
        HttpService httpService,
        ConnectivityService connectivityService,
        VaultSessionService vaultSessionService,
        ISyncingService syncingService,
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
                logger?.LogInformation("Login attempt for {Email}", email);
                // Use GetSection and check for null or empty value to avoid CS8600
                var urlSection = configuration.GetSection("GOATVAULT_SERVER_BASE_URL");
                var url = urlSection.Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    await Shell.Current.DisplayAlertAsync("Configuration Error", "Server base URL is not configured.", "OK");
                    return false;
                }

                // Double-check connectivity before network call
                var hasConnection = connectivityService.CheckConnectivity();
                if (!hasConnection)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Connection Error",
                        "Unable to verify internet connection.",
                        "OK");
                    return false;
                }

                // Init Auth
                var initPayload = new AuthInitRequest { Email = email };
                var initResponse = await httpService.PostAsync<AuthInitResponse>(
                    $"{url}v1/auth/init",
                    initPayload
                );

                // Generate Verifier
                var loginVerifier = CryptoService.GenerateAuthVerifier(password, initResponse.AuthSalt);

                // Check if MFA is required
                string? mfaCode = null;
                if (initResponse.MfaEnabled)
                {
                    if (mfaCodeProvider == null)
                    {
                        // Should not happen if VM provides it
                        throw new InvalidOperationException("MFA is enabled but no code provider was supplied.");
                    }

                    mfaCode = await mfaCodeProvider();

                    if (string.IsNullOrWhiteSpace(mfaCode))
                    {
                        // User cancelled MFA prompt
                        return false;
                    }
                }

                // Verify
                var verifyPayload = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(initResponse.UserId),
                    AuthVerifier = loginVerifier,
                    MfaCode = mfaCode
                };

                var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                    $"{url}v1/auth/verify",
                    verifyPayload
                );

                // Store tokens and session data
                authTokenService.SetToken(verifyResponse.AccessToken);
                authTokenService.SetRefreshToken(verifyResponse.RefreshToken);

                // Store master password in session for vault decryption
                vaultSessionService.MasterPassword = password;

                // Get User Data
                var userResponse = await httpService.GetAsync<UserResponse>(
                    $"{url}v1/users/{initResponse.UserId}"
                );

                // Set the current user
                vaultSessionService.CurrentUser = userResponse;

                // Sync Local DB (Delete old if exists, save new)
                var existingUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

                // Decrypt & Store Session
                vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, password);

                logger?.LogInformation("Login successful for user {UserId}", vaultSessionService.CurrentUser.Id);
                return true;
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                logger?.LogWarning("Login failed for {Email}: unauthorized", email);
                await Shell.Current.DisplayAlertAsync("Login Failed", "Invalid email or password (or MFA code). Please try again.", "OK");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger?.LogWarning("Login failed for {Email}: account not found", email);
                await Shell.Current.DisplayAlertAsync("Account Not Found", "No account found with this email address.", "OK");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                logger?.LogWarning("Login failed for {Email}: bad request", email);
                await Shell.Current.DisplayAlertAsync("Invalid Request", "Please check your email and password.", "OK");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                logger?.LogWarning("Login failed for {Email}: conflict", email);
                await Shell.Current.DisplayAlertAsync("Error", "This email is already registered.", "OK");
            }
            catch (HttpRequestException httpEx)
            {
                logger?.LogError(httpEx, "Login connection error for {Email}", email);
                // Network-related errors
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    $"Unable to connect to the server. {httpEx.Message}",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                logger?.LogWarning("Login timed out for {Email}", email);
                // Timeout errors
                await Shell.Current.DisplayAlertAsync(
                    "Timeout",
                    "The request timed out. Please try again.",
                    "OK");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("decrypt"))
            {
                logger?.LogError(ex, "Vault decryption failed during login for {Email}", email);
                await Shell.Current.DisplayAlertAsync(
                    "Decryption Error",
                    "Unable to decrypt your vault. This may indicate a data corruption issue.",
                    "OK");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error during login for {Email}", email);
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }

            return false;
        }
        public async Task<bool> LoginOfflineAsync(string? email, string? password, DbModel? selectedAccount)
        {
            // Validation
            if (selectedAccount == null)
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
                logger?.LogInformation("Offline login attempt for account {AccountId}", selectedAccount.Id);

                // 1. Load the user from local database
                var dbUser = await vaultService.LoadUserFromLocalAsync(selectedAccount.Id);

                if (dbUser == null)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Account not found in local storage.", "OK");
                    return false;
                }

                var decryptedVault = vaultService.DecryptVault(dbUser.Vault, password);

                // Password is correct!
                vaultSessionService.MasterPassword = password;
                vaultSessionService.DecryptedVault = decryptedVault;

                // Set current user in session (convert DbModel to UserResponse format)
                vaultSessionService.CurrentUser = new UserResponse
                {
                    Id = dbUser.Id,
                    Email = dbUser.Email,
                    AuthSalt = dbUser.AuthSalt,
                    MfaEnabled = dbUser.MfaEnabled,
                    Vault = dbUser.Vault,
                    CreatedAt = dbUser.CreatedAt,
                    UpdatedAt = dbUser.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                // Decryption failed - wrong password
                logger?.LogWarning(ex, "Offline login failed for account {AccountId} — likely wrong password", selectedAccount.Id);
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    "Incorrect password for this account.",
                    "OK");
                return false;
            }
            return true;
        }
        public async Task RegisterAsync(string email, string? password, string? confirmPassword)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
                return;
            }

            // Password Match
            if (password != confirmPassword)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Passwords do not match.", "OK");
                return;
            }

            try
            {
                logger?.LogInformation("Registration attempt.");
                // Use GetSection and check for null or empty value to avoid CS8600
                var urlSection = configuration.GetSection("GOATVAULT_SERVER_BASE_URL");
                var url = urlSection.Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                // Check connectivity before network call
                var hasConnection = connectivityService.CheckConnectivity();
                if (!hasConnection)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Connection Error",
                        "Unable to verify internet connection.",
                        "OK");
                    return;
                }

                // Generate Auth Salt & Verifier
                var authSalt = CryptoService.GenerateAuthSalt();
                var authVerifier = CryptoService.HashPassword(password, authSalt);

                var registerRequest = new AuthRegisterRequest
                {
                    AuthSalt = Convert.ToBase64String(authSalt),
                    AuthVerifier = authVerifier,
                    Email = email,
                    Vault = null,
                };

                // Encrypt vault (Initial empty vault)
                var vaultPayload = vaultService.EncryptVault(password, null);
                registerRequest.Vault = vaultPayload;

                // API: Register
                var registerResponse = await httpService.PostAsync<AuthRegisterResponse>(
                    $"{url}v1/auth/register",
                    registerRequest
                );

                // API: Verify (Get Token)
                var verifyRequest = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(registerResponse.Id),
                    AuthVerifier = registerRequest.AuthVerifier
                };

                var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                    $"{url}v1/auth/verify",
                    verifyRequest
                );

                authTokenService.SetToken(verifyResponse.AccessToken);
                vaultSessionService.MasterPassword = password;

                // API: Get User Profile
                var userResponse = await httpService.GetAsync<UserResponse>(
                    $"{url}v1/users/{registerResponse.Id}"
                );

                // Update session with new user
                vaultSessionService.CurrentUser = userResponse;

                // Save new user to SQLite
                await vaultService.SaveUserToLocalAsync(new DbModel
                {
                    Id = vaultSessionService.CurrentUser.Id,
                    Email = vaultSessionService.CurrentUser.Email,
                    AuthSalt = vaultSessionService.CurrentUser.AuthSalt,
                    MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
                    Vault = vaultSessionService.CurrentUser.Vault
                });

                // Decrypt & Store Session in RAM
                vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, password);

                logger?.LogInformation("Registration successful for user {UserId}", vaultSessionService.CurrentUser.Id);
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
        }
        public async Task LogoutAsync()
        {
            try
            {
                logger?.LogInformation("Logout initiated");

                // Confirm logout action
                var confirmPopup = new PromptPopup(
                    title: "Logout",
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

                // Save vault before logout
                await syncingService.Save();

                // Clear auth token
                authTokenService.SetToken(string.Empty);

                // Lock session
                vaultSessionService.Lock();

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
                    title: "Error",
                    body: "Failed to logout properly. Please try again.",
                    aText: "OK"
                ));
            }
        }
        public async Task<List<DbModel>> GetAllLocalAccountsAsync()
        {
            var accounts = await vaultService.LoadAllUsersFromLocalAsync();
            return accounts;
        }
        public async Task RemoveLocalAccountAsync(DbModel account)
        {
            // Confirm deletion
            var confirmPopup = new PromptPopup(
                    title: "Remove Local Account",
                    body: "Are you sure you want to delete this account from local storage? All changes will be saved.",
                    aText: "Delete",
                    cText: "Cancel"
                );
            await MopupService.Instance.PushAsync(confirmPopup);
            var confirm = await confirmPopup.WaitForScan();

            if (!confirm)
                return;

            // Remove from local database
            await vaultService.DeleteUserFromLocalAsync(account.Id);
        }
    }
}
