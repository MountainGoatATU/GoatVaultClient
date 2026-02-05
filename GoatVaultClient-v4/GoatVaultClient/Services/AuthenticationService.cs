using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;

namespace GoatVaultClient.Services
{
    public interface IAuthenticationService
    {
        public Task LoginAsync(string email, string password);
        public Task LoginOfflineAsync (string email, string password, DbModel selectedAccount);
        public Task RegisterAsync(string email, string password, string cofirmPassword);
        public Task<bool> LogoutAsync();
        public Task<bool> LogoutOfflineAsync();
        public Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
        public Task<bool> ChangeEmailAsync();
        public Task<List<DbModel>> GetAllLocalAccountsAsync();
        public Task<bool> RemoveLocalAccountAsync(string email);
    }
    public class AuthenticationService(
        IConfiguration configuration,
        VaultService vaultService,
        AuthTokenService authTokenService,
        HttpService httpService,
        ConnectivityService connectivityService,
        VaultSessionService vaultSessionService
        ) : IAuthenticationService
    {
        public async Task LoginAsync(string email, string password)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
                return;
            }
            try
            {
                // Use GetSection and check for null or empty value to avoid CS8600
                var urlSection = configuration.GetSection("GOATVAULT_SERVER_BASE_URL");
                string? url = urlSection.Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }
                // Double-check connectivity before network call
                var hasConnection = await connectivityService.CheckConnectivityAsync();
                if (!hasConnection)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Connection Error",
                        "Unable to verify internet connection.",
                        "OK");
                    return;
                }

                // Init Auth
                var initPayload = new AuthInitRequest { Email = email };
                var initResponse = await httpService.PostAsync<AuthInitResponse>(
                    $"{url}v1/auth/init",
                    initPayload
                );

                // Generate Verifier
                var loginVerifier = CryptoService.GenerateAuthVerifier(password, initResponse.AuthSalt);

                // Verify
                var verifyPayload = new AuthVerifyRequest
                {
                    UserId = Guid.Parse(initResponse.UserId),
                    AuthVerifier = loginVerifier
                };
                var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                    $"{url}v1/auth/verify",
                    verifyPayload
                );
                authTokenService.SetToken(verifyResponse.AccessToken);
                vaultSessionService.MasterPassword = password;

                // Get User Data
                var userResponse = await httpService.GetAsync<UserResponse>(
                    $"{url}v1/users/{initResponse.UserId}"
                );

                // Set the current user
                vaultSessionService.CurrentUser = userResponse;

                // Decrypt & Store Session
                vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, password);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await Shell.Current.DisplayAlertAsync("Error", "This email is already registered.", "OK");
            }
            catch (HttpRequestException)
            {
                // Network-related errors
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    "Unable to connect to the server. Please check your internet connection.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                // Timeout errors
                await Shell.Current.DisplayAlertAsync(
                    "Timeout",
                    "The request timed out. Please try again.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        public async Task LoginOfflineAsync(string email, string password, DbModel selectedAccount)
        {
            // Validation
            if (selectedAccount == null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please select an account.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Password is required.", "OK");
                return;
            }

            try
            {
                // 1. Load the user from local database
                var dbUser = await vaultService.LoadUserFromLocalAsync(selectedAccount.Id);

                if (dbUser == null)
                {
                    await Shell.Current.DisplayAlertAsync("Error", "Account not found in local storage.", "OK");
                    return;
                }

                // 3. Try to decrypt the vault - if this succeeds, password is correct
                try
                {
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
                catch
                {
                    // Decryption failed - wrong password
                    await Shell.Current.DisplayAlertAsync(
                        "Error",
                        "Incorrect password for this account.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        public async Task RegisterAsync(string email, string password, string confirmPassword)
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
                // Use GetSection and check for null or empty value to avoid CS8600
                var urlSection = configuration.GetSection("GOATVAULT_SERVER_BASE_URL");
                string? url = urlSection.Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                // Check connectivity before network call
                var hasConnection = await connectivityService.CheckConnectivityAsync();
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
            }
            catch (HttpRequestException)
            {
                // Network-related errors
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    "Unable to connect to the server. Please check your internet connection.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                // Timeout errors
                await Shell.Current.DisplayAlertAsync(
                    "Timeout",
                    "The request timed out. Please try again.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        public async Task<bool> LogoutAsync()
        {
            throw new NotImplementedException();
        }
        public async Task<bool> LogoutOfflineAsync()
        {
            throw new NotImplementedException();

        }
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }
        public async Task<bool> ChangeEmailAsync()
        {
            throw new NotImplementedException();
        }
        public async Task<List<DbModel>> GetAllLocalAccountsAsync()
        {
            var accounts = await vaultService.LoadAllUsersFromLocalAsync();
            return accounts;
        }
        public async Task<bool> RemoveLocalAccountAsync(string email)
        {
            throw new NotImplementedException();
        }
    }
}
