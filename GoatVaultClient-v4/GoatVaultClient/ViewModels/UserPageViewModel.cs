using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Services;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;
using OtpNet;

namespace GoatVaultClient.ViewModels
{
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email;
        [ObservableProperty] private double vaultScore;
        [ObservableProperty] private bool mfaEnabled;
        [ObservableProperty] private string? mfaSecret;
        [ObservableProperty] private string? mfaQrCodeUrl;
        [ObservableProperty] private bool goatEnabled = true;

        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultSessionService _vaultSessionService;
        private readonly VaultService _vaultService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly GoatTipsService _goatTipsService;

        public UserPageViewModel(
            HttpService httpService,
            AuthTokenService authTokenService,
            VaultSessionService vaultSessionService,
            VaultService vaultService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            GoatTipsService goatTipsService)
        {
            _httpService = httpService;
            _authTokenService = authTokenService;
            _vaultSessionService = vaultSessionService;
            _vaultService = vaultService;
            _configuration = configuration;
            _goatTipsService = goatTipsService;

            // Read once from service (single source of truth)
            GoatEnabled = _goatTipsService.IsGoatEnabled;
            _goatTipsService.SetEnabled(GoatEnabled);

            if (_vaultSessionService.CurrentUser == null)
                return;

            Email = _vaultSessionService.CurrentUser.Email;
            MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled;
        }

        [RelayCommand]
        private void ToggleGoat()
        {
            // Persisted via GoatTipsService to device storage.
            GoatEnabled = !GoatEnabled;
            _goatTipsService.SetEnabled(GoatEnabled);
        }

        [RelayCommand]
        private async Task EnableMfaAsync()
        {
            System.Diagnostics.Debug.WriteLine("EnableMfaAsync called");

            var user = _vaultSessionService.CurrentUser;
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User is null");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "No user logged in.",
                    aText: "OK"
                ));
                return;
            }

            try
            {
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine("Starting MFA enable process");

                // Ask for current password to authorize
                System.Diagnostics.Debug.WriteLine("Prompting for password");
                var password = await PromptUserAsync("Confirm Password", true);

                if (password == null)
                {
                    System.Diagnostics.Debug.WriteLine("Password prompt cancelled");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Password entered, authorizing...");

                // Verify with server
                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                {
                    System.Diagnostics.Debug.WriteLine("Authorization failed");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Authorization successful");

                // Wait for password popup to fully dismiss
                await Task.Delay(300);

                // Generate new TOTP secret
                System.Diagnostics.Debug.WriteLine("Generating TOTP secret");
                var secretBytes = KeyGeneration.GenerateRandomKey(20); // 160 bits
                var secret = Base32Encoding.ToString(secretBytes);
                MfaSecret = secret;

                System.Diagnostics.Debug.WriteLine($"Generated secret: {secret}");

                // Generate QR code URL for authenticator apps
                const string issuer = "GoatVault";
                MfaQrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(Email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

                System.Diagnostics.Debug.WriteLine($"QR Code URL: {MfaQrCodeUrl}");

                // Show QR code and secret to user
                await ShowMfaSetupDialog();

                // Small delay before verification code prompt
                await Task.Delay(300);

                // Ask user to enter a code to verify they've set it up
                System.Diagnostics.Debug.WriteLine("Prompting for verification code");
                var verificationCode = await PromptUserAsync("Enter 6-digit code from authenticator app", false);

                if (string.IsNullOrWhiteSpace(verificationCode))
                {
                    System.Diagnostics.Debug.WriteLine("Verification cancelled");
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Setup Cancelled",
                        body: "MFA setup was cancelled.",
                        aText: "OK"
                    ));
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Verification code entered: {verificationCode}");

                // Verify the code
                var isValid = TotpService.VerifyCode(secret, verificationCode);
                System.Diagnostics.Debug.WriteLine($"Code verification result: {isValid}");

                if (!isValid)
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Invalid Code",
                        body: "The code you entered is incorrect. Please try again.",
                        aText: "OK"
                    ));
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Code verified, updating server...");

                var authSaltBytes = Convert.FromBase64String(user.AuthSalt);
                var authVerifier = CryptoService.HashPassword(password, authSaltBytes);

                System.Diagnostics.Debug.WriteLine($"DEBUG MFA Enable - About to PATCH:");
                System.Diagnostics.Debug.WriteLine($"  auth_salt: {user.AuthSalt}");
                System.Diagnostics.Debug.WriteLine($"  auth_verifier: {authVerifier}");
                System.Diagnostics.Debug.WriteLine($"  email: {user.Email}");
                System.Diagnostics.Debug.WriteLine($"  mfa_enabled: {user.MfaEnabled}");
                System.Diagnostics.Debug.WriteLine($"  mfa_secret: {secret}");
                System.Diagnostics.Debug.WriteLine($"  vault keys: {user.Vault != null}");

                var baseUrl = _configuration["GOATVAULT_SERVER_BASE_URL"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new Exception("Server base URL not configured");
                }

                // Update server with MFA enabled and secret
                var request = new UserRequest
                {
                    Email = user.Email,
                    MfaEnabled = true,
                    MfaSecret = secret,
                    Vault = user.Vault
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                System.Diagnostics.Debug.WriteLine($"DEBUG MFA Enable - Server response:");
                System.Diagnostics.Debug.WriteLine($"  mfa_enabled: {updatedUser.MfaEnabled}");
                System.Diagnostics.Debug.WriteLine($"  mfa_secret: {updatedUser.MfaSecret}");


                // Update local database
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.MfaEnabled = true;
                    dbUser.MfaSecret = secret;
                    await _vaultService.UpdateUserInLocalAsync(dbUser);
                    System.Diagnostics.Debug.WriteLine("Local database updated");
                }

                // Update session
                _vaultSessionService.CurrentUser.MfaEnabled = true;
                _vaultSessionService.CurrentUser.MfaSecret = secret;
                MfaEnabled = true;

                System.Diagnostics.Debug.WriteLine("MFA enabled successfully!");

                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "MFA Enabled",
                    body: "Two-factor authentication has been successfully enabled for your account.",
                    aText: "OK"
                ));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling MFA: {ex}");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: $"Failed to enable MFA: {ex.Message}",
                    aText: "OK"
                ));
            }
            finally
            {
                IsBusy = false;
            }
        }


        [RelayCommand]
        private async Task DisableMfaAsync()
        {
            System.Diagnostics.Debug.WriteLine("DisableMfaAsync called");

            var user = _vaultSessionService.CurrentUser;
            if (user == null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Error",
                        body: "No user logged in.",
                        aText: "OK"
                    ));
                });
                return;
            }

            try
            {
                IsBusy = true;

                // Confirm action
                bool confirm = false;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var confirmPopup = new PromptPopup(
                        title: "Disable MFA",
                        body: "Are you sure you want to disable two-factor authentication?",
                        aText: "Disable",
                        cText: "Cancel"
                    );
                    await MopupService.Instance.PushAsync(confirmPopup);
                    confirm = await confirmPopup.WaitForScan();
                });

                if (!confirm)
                {
                    IsBusy = false;
                    return;
                }

                // Wait for confirmation popup to fully close
                await Task.Delay(500);

                // Ask for current password
                System.Diagnostics.Debug.WriteLine("Prompting for password");
                string? password = null;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    password = await PromptUserAsync("Confirm Password", true);
                });

                if (password == null)
                {
                    IsBusy = false;
                    return;
                }

                // Verify locally
                System.Diagnostics.Debug.WriteLine("Verifying password locally");
                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                {
                    System.Diagnostics.Debug.WriteLine("Local password verification failed");
                    IsBusy = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Password verified");

                // Small delay before next prompt
                await Task.Delay(300);

                // Ask for current MFA code to verify
                System.Diagnostics.Debug.WriteLine("Prompting for MFA code");
                string? mfaCode = null;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var mfaPopup = new AuthorizePopup("Enter your current 6-digit MFA code", isPassword: false);
                    await MopupService.Instance.PushAsync(mfaPopup);
                    mfaCode = await mfaPopup.WaitForScan();
                });

                System.Diagnostics.Debug.WriteLine($"MFA code returned: '{mfaCode}' (length: {mfaCode?.Length ?? 0})");

                if (string.IsNullOrWhiteSpace(mfaCode))
                {
                    System.Diagnostics.Debug.WriteLine("MFA code prompt cancelled or empty");
                    IsBusy = false;
                    return;
                }

                // Load MFA secret from local DB to verify the code
                System.Diagnostics.Debug.WriteLine("Loading MFA secret from local DB");
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser == null || string.IsNullOrWhiteSpace(dbUser.MfaSecret))
                {
                    System.Diagnostics.Debug.WriteLine("MFA secret not found in local DB");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MopupService.Instance.PushAsync(new PromptPopup(
                            title: "Error",
                            body: "MFA secret not found. Please try logging out and back in.",
                            aText: "OK"
                        ));
                    });
                    IsBusy = false;
                    return;
                }

                // Verify the MFA code
                System.Diagnostics.Debug.WriteLine($"Verifying MFA code against secret");
                var isValidCode = TotpService.VerifyCode(dbUser.MfaSecret, mfaCode);
                if (!isValidCode)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid MFA code");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MopupService.Instance.PushAsync(new PromptPopup(
                            title: "Invalid Code",
                            body: "The MFA code you entered is incorrect. Please try again.",
                            aText: "OK"
                        ));
                    });
                    IsBusy = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MFA code verified successfully");

                // Generate auth_verifier from current password for backend
                var authSaltBytes = Convert.FromBase64String(user.AuthSalt);
                var authVerifier = CryptoService.HashPassword(password, authSaltBytes);

                // Update server with MFA disabled
                System.Diagnostics.Debug.WriteLine("Sending PATCH request to disable MFA");

                var baseUrl = _configuration["GOATVAULT_SERVER_BASE_URL"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new Exception("Server base URL not configured");
                }

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    new
                    {
                        auth_salt = user.AuthSalt,
                        auth_verifier = authVerifier,
                        email = user.Email,
                        vault = user.Vault,
                        mfa_enabled = false,
                        mfa_secret = (string?)null,
                        mfa_code = mfaCode
                    }
                );

                System.Diagnostics.Debug.WriteLine("Server updated successfully");

                // Update local database
                dbUser.MfaEnabled = false;
                dbUser.MfaSecret = null;
                await _vaultService.UpdateUserInLocalAsync(dbUser);

                System.Diagnostics.Debug.WriteLine("Local DB updated");

                // Update session
                _vaultSessionService.CurrentUser.MfaEnabled = false;
                _vaultSessionService.CurrentUser.MfaSecret = null;
                MfaEnabled = false;

                System.Diagnostics.Debug.WriteLine("MFA disabled successfully!");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "MFA Disabled",
                        body: "Two-factor authentication has been successfully disabled.",
                        aText: "OK"
                    ));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling MFA: {ex}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Error",
                        body: $"Failed to disable MFA: {ex.Message}",
                        aText: "OK"
                    ));
                });
            }
            finally
            {
                IsBusy = false;
            }
        }


        private async Task ShowMfaSetupDialog()
        {
            var message = $"Scan this QR code with your authenticator app:\n\n" +
                          $"Or enter this secret manually:\n{MfaSecret}\n\n" +
                          $"After adding to your app, you'll need to enter a code to verify.\n\n" +
                          $"Recommended apps:\n" +
                          $"• Google Authenticator\n" +
                          $"• Microsoft Authenticator\n" +
                          $"• Authy";

            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Setup MFA",
                body: message,
                aText: "OK"
            ));

            // Small delay
            await Task.Delay(300);

            // Copy secret to clipboard for easy entry
            await Clipboard.Default.SetTextAsync(MfaSecret ?? "");

            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Secret Copied",
                body: "The secret has been copied to your clipboard.",
                aText: "OK"
            ));
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            System.Diagnostics.Debug.WriteLine("EditEmailAsync called");

            var user = _vaultSessionService.CurrentUser;
            if (user == null)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "No user logged in.",
                    aText: "OK"
                ));
                return;
            }

            try
            {
                IsBusy = true;

                // Ask for current password
                System.Diagnostics.Debug.WriteLine("Prompting for password");
                var enteredPassword = await PromptUserAsync("Confirm Password", true);
                if (enteredPassword == null)
                {
                    System.Diagnostics.Debug.WriteLine("Password prompt cancelled");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Verifying password");
                // Verify password
                var authorized = await AuthorizeAsync(enteredPassword);
                if (!authorized)
                {
                    System.Diagnostics.Debug.WriteLine("Authorization failed");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Password verified, waiting before next prompt");

                // Small delay to ensure first popup is fully dismissed
                await Task.Delay(300);

                // Ask for new email
                System.Diagnostics.Debug.WriteLine("Prompting for new email");
                var newEmail = await PromptUserAsync("Enter new email", false);

                System.Diagnostics.Debug.WriteLine($"New email received: '{newEmail}'");

                if (string.IsNullOrWhiteSpace(newEmail) || newEmail == user.Email)
                {
                    System.Diagnostics.Debug.WriteLine("Email unchanged or cancelled");
                    return;
                }

                var oldEmail = user.Email;

                System.Diagnostics.Debug.WriteLine($"Updating email from '{oldEmail}' to '{newEmail}'");

                var baseUrl = _configuration["GOATVAULT_SERVER_BASE_URL"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new Exception("Server base URL not configured");
                }

                // Update Server
                var request = new UserRequest
                {
                    Email = newEmail,
                    MfaEnabled = user.MfaEnabled,
                    Vault = user.Vault
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                System.Diagnostics.Debug.WriteLine("Server updated successfully");

                // Update local database
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.Email = newEmail;
                    await _vaultService.UpdateUserInLocalAsync(dbUser);
                    System.Diagnostics.Debug.WriteLine("Local database updated");
                }

                // Update session and UI
                _vaultSessionService.CurrentUser.Email = updatedUser.Email;
                Email = updatedUser.Email;

                System.Diagnostics.Debug.WriteLine("Session updated");

                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Success",
                    body: "Email updated successfully.",
                    aText: "OK"
                ));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating email: {ex}");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: $"Failed to update email: {ex.Message}",
                    aText: "OK"
                ));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditMasterPasswordAsync()
        {
            var user = _vaultSessionService.CurrentUser;
            if (user == null)
                return;

            try
            {
                IsBusy = true;

                // Ask for current password
                System.Diagnostics.Debug.WriteLine("Prompting for current password");
                var password = await PromptUserAsync("Confirm Password", true);
                if (password == null)
                {
                    System.Diagnostics.Debug.WriteLine("Password prompt cancelled");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Verifying password");
                // Verify password
                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                {
                    System.Diagnostics.Debug.WriteLine("Authorization failed");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Password verified, waiting before next prompt");

                // Small delay to ensure first popup is fully dismissed
                await Task.Delay(300);

                // Ask for new password
                System.Diagnostics.Debug.WriteLine("Prompting for new password");
                var newPassword = await PromptUserAsync("Enter new master password", true);

                System.Diagnostics.Debug.WriteLine($"New password received: {(string.IsNullOrWhiteSpace(newPassword) ? "NULL/EMPTY" : "OK")}");

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    System.Diagnostics.Debug.WriteLine("New password is empty, cancelling");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Re-encrypting vault with new password");
                // Re-encrypt vault with new password
                var newVaultModel = _vaultService.EncryptVault(newPassword, _vaultSessionService.DecryptedVault);

                System.Diagnostics.Debug.WriteLine("Generating new auth credentials");
                // Generate new authentication credentials
                var newAuthSalt = CryptoService.GenerateAuthSalt();
                var newAuthVerifier = CryptoService.HashPassword(newPassword, newAuthSalt);
                var newAuthSaltBase64 = Convert.ToBase64String(newAuthSalt);

                // Update server with new vault AND new auth credentials
                System.Diagnostics.Debug.WriteLine("Updating server with new vault and auth credentials");

                var baseUrl = _configuration["GOATVAULT_SERVER_BASE_URL"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new Exception("Server base URL not configured");
                }

                var request = new
                {
                    auth_salt = newAuthSaltBase64,
                    auth_verifier = newAuthVerifier,
                    email = user.Email,
                    mfa_enabled = user.MfaEnabled,
                    mfa_secret = user.MfaEnabled ? _vaultSessionService.CurrentUser?.MfaSecret : null,
                    vault = newVaultModel
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                System.Diagnostics.Debug.WriteLine("Server updated successfully");

                // Update local database
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.AuthSalt = newAuthSaltBase64;
                    dbUser.Vault = newVaultModel;
                    dbUser.MfaSecret = user.MfaEnabled ? _vaultSessionService.CurrentUser?.MfaSecret : null;
                    await _vaultService.UpdateUserInLocalAsync(dbUser);
                    System.Diagnostics.Debug.WriteLine("Local database updated");
                }

                // Update session
                _vaultSessionService.CurrentUser.AuthSalt = newAuthSaltBase64;
                _vaultSessionService.CurrentUser.Vault = newVaultModel;
                _vaultSessionService.MasterPassword = newPassword;

                System.Diagnostics.Debug.WriteLine("Session updated with new password and auth credentials");

                // Confirmation
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Success",
                    body: "Master password updated successfully. You can now login with your new password.",
                    aText: "OK"
                ));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating master password: {ex}");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: $"Failed to update master password: {ex.Message}",
                    aText: "OK"
                ));
            }
            finally
            {
                IsBusy = false;
            }
        }


        // Method to show popup and get user input
        private async Task<string?> PromptUserAsync(string title, bool isPassword)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PromptUserAsync called - Title: '{title}', IsPassword: {isPassword}");

                var popup = new AuthorizePopup(title, isPassword: isPassword);
                System.Diagnostics.Debug.WriteLine("Popup created");

                await MopupService.Instance.PushAsync(popup);
                System.Diagnostics.Debug.WriteLine("Popup pushed to screen");

                var result = await popup.WaitForScan();
                System.Diagnostics.Debug.WriteLine($"Popup result received: '{result}' (IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(result)})");

                var returnValue = string.IsNullOrWhiteSpace(result) ? null : result;
                System.Diagnostics.Debug.WriteLine($"Returning: {(returnValue == null ? "NULL" : $"'{returnValue}'")}");

                return returnValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PromptUserAsync: {ex}");
                return null;
            }
        }

        private async Task<bool> AuthorizeAsync(string? enteredPassword = null)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                System.Diagnostics.Debug.WriteLine("Authorization cancelled - no password entered");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("===== AUTHORIZATION START =====");
                System.Diagnostics.Debug.WriteLine("Verifying password against stored master password...");

                // Verify the entered password matches the stored master password
                if (enteredPassword != _vaultSessionService.MasterPassword)
                {
                    System.Diagnostics.Debug.WriteLine("Password does not match stored master password");
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Incorrect Password",
                        body: "The password you entered is incorrect. Please try again.",
                        aText: "OK"
                    ));
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("Password verified successfully!");
                System.Diagnostics.Debug.WriteLine("===== AUTHORIZATION SUCCESS =====");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authorization failed with exception: {ex}");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: $"An error occurred during authorization: {ex.Message}",
                    aText: "OK"
                ));
                return false;
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                IsBusy = true;

                var confirmPopup = new PromptPopup(
                    title: "Logout",
                    body: "Are you sure you want to logout? All changes will be saved.",
                    aText: "Logout",
                    cText: "Cancel"
                );
                await MopupService.Instance.PushAsync(confirmPopup);
                var confirm = await confirmPopup.WaitForScan();

                if (!confirm)
                    return;

                System.Diagnostics.Debug.WriteLine("Logging out - saving vault...");

                // Save vault before logout
                if (_vaultSessionService.DecryptedVault != null &&
                    !string.IsNullOrEmpty(_vaultSessionService.MasterPassword) &&
                    _vaultSessionService.CurrentUser != null)
                {
                    await _vaultService.SaveVaultAsync(
                        _vaultSessionService.CurrentUser,
                        _vaultSessionService.MasterPassword,
                        _vaultSessionService.DecryptedVault);

                    System.Diagnostics.Debug.WriteLine("Vault saved successfully");
                }

                // Clear auth token
                _authTokenService.SetToken(null);

                // Lock session
                _vaultSessionService.Lock();

                // Navigate to login/intro page
                if (Application.Current != null)
                {
                    Application.Current.MainPage = new AppShell();
                }

                await Shell.Current.GoToAsync("//IntroductionPage");

                System.Diagnostics.Debug.WriteLine("Logout complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex}");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "Failed to logout properly. Please try again.",
                    aText: "OK"
                ));
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
