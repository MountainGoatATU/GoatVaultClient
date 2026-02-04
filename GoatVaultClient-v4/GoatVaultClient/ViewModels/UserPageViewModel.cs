using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
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

        // Services
        private readonly HttpService _httpService;
        private readonly AuthTokenService _authTokenService;
        private readonly VaultSessionService _vaultSessionService;
        private readonly VaultService _vaultService;

        // Constructor
        public UserPageViewModel(
            HttpService httpService,
            AuthTokenService authTokenService,
            VaultSessionService vaultSessionService,
            VaultService vaultService)
        {
            _httpService = httpService;
            _authTokenService = authTokenService;
            _vaultSessionService = vaultSessionService;
            _vaultService = vaultService;

            // Initialize properties from current session
            if (_vaultSessionService.CurrentUser == null)
                return;

            Email = _vaultSessionService.CurrentUser.Email;
            MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled;
        }

        [RelayCommand]
        private async Task EnableMfaAsync()
        {
            System.Diagnostics.Debug.WriteLine("EnableMfaAsync called");

            var user = _vaultSessionService.CurrentUser;
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User is null");
                await Shell.Current.DisplayAlertAsync("Error", "No user logged in.", "OK");
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

                // Ask user to enter a code to verify they've set it up
                System.Diagnostics.Debug.WriteLine("Prompting for verification code");
                var verificationCode = await PromptUserAsync("Enter 6-digit code from authenticator app", false);

                if (string.IsNullOrWhiteSpace(verificationCode))
                {
                    System.Diagnostics.Debug.WriteLine("Verification cancelled");
                    await Shell.Current.DisplayAlertAsync(
                        "Setup Cancelled",
                        "MFA setup was cancelled.",
                        "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Verification code entered: {verificationCode}");

                // Verify the code
                var isValid = TotpService.VerifyCode(secret, verificationCode);
                System.Diagnostics.Debug.WriteLine($"Code verification result: {isValid}");

                if (!isValid)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Invalid Code",
                        "The code you entered is incorrect. Please try again.",
                        "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Code verified, updating server...");

                // Update server with MFA enabled and secret
                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{user.Id}",
                    new
                    {
                        mfa_enabled = true,
                        mfa_secret = secret
                    }
                );

                System.Diagnostics.Debug.WriteLine("Server updated successfully");

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
                MfaEnabled = true;

                System.Diagnostics.Debug.WriteLine("MFA enabled successfully!");

                await Shell.Current.DisplayAlertAsync(
                    "MFA Enabled",
                    "Two-factor authentication has been successfully enabled for your account.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling MFA: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Failed to enable MFA: {ex.Message}",
                    "OK");
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
                await Shell.Current.DisplayAlertAsync("Error", "No user logged in.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Confirm action
                var confirm = await Shell.Current.DisplayAlertAsync(
                    "Disable MFA",
                    "Are you sure you want to disable two-factor authentication?",
                    "Disable",
                    "Cancel");

                if (!confirm)
                    return;

                // Ask for current password
                System.Diagnostics.Debug.WriteLine("Prompting for password");
                var password = await PromptUserAsync("Confirm Password", true);
                if (password == null) return;

                // Verify locally
                System.Diagnostics.Debug.WriteLine("Verifying password locally");
                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                {
                    System.Diagnostics.Debug.WriteLine("Local password verification failed");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Password verified");

                // Ask for current MFA code to verify
                System.Diagnostics.Debug.WriteLine("Prompting for MFA code");
                var mfaCode = await Shell.Current.DisplayPromptAsync(
                    "MFA Code Required",
                    "Enter your current 6-digit MFA code to confirm:",
                    "Verify",
                    "Cancel",
                    placeholder: "000000",
                    maxLength: 6,
                    keyboard: Keyboard.Numeric
                );
                System.Diagnostics.Debug.WriteLine($"MFA code returned: '{mfaCode}' (length: {mfaCode?.Length ?? 0})");

                if (string.IsNullOrWhiteSpace(mfaCode))
                {
                    System.Diagnostics.Debug.WriteLine("MFA code prompt cancelled or empty");
                    return;
                }

                // Load MFA secret from local DB to verify the code
                System.Diagnostics.Debug.WriteLine("Loading MFA secret from local DB");
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser == null || string.IsNullOrWhiteSpace(dbUser.MfaSecret))
                {
                    System.Diagnostics.Debug.WriteLine("MFA secret not found in local DB");
                    await Shell.Current.DisplayAlertAsync(
                        "Error",
                        "MFA secret not found. Please try logging out and back in.",
                        "OK");
                    return;
                }

                // Verify the MFA code
                System.Diagnostics.Debug.WriteLine($"Verifying MFA code against secret");
                var isValidCode = TotpService.VerifyCode(dbUser.MfaSecret, mfaCode);
                if (!isValidCode)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid MFA code");
                    await Shell.Current.DisplayAlertAsync(
                        "Invalid Code",
                        "The MFA code you entered is incorrect. Please try again.",
                        "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MFA code verified successfully");

                // Update server with MFA disabled
                // We need to include the MFA code in the request or header
                System.Diagnostics.Debug.WriteLine("Sending PATCH request to disable MFA");

                // First, set the MFA code in the Authorization header for this request
                // (assuming your backend checks the X-MFA-Code header)
                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{user.Id}",
                    new
                    {
                        mfa_enabled = false,
                        mfa_secret = (string?)null,
                        mfa_code = mfaCode  // Include in request body
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
                MfaEnabled = false;
                MfaSecret = null;

                System.Diagnostics.Debug.WriteLine("Session updated");

                await Shell.Current.DisplayAlertAsync(
                    "MFA Disabled",
                    "Two-factor authentication has been disabled for your account.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling MFA: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Failed to disable MFA: {ex.Message}",
                    "OK");
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

            await Shell.Current.DisplayAlertAsync(
                "Setup MFA",
                message,
                "OK");

            // Copy secret to clipboard for easy entry
            await Clipboard.Default.SetTextAsync(MfaSecret ?? "");
            await Shell.Current.DisplayAlertAsync(
                "Secret Copied",
                "The secret has been copied to your clipboard.",
                "OK");
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            System.Diagnostics.Debug.WriteLine("EditEmailAsync called");

            var user = _vaultSessionService.CurrentUser;
            if (user == null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "No user logged in.", "OK");
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

                System.Diagnostics.Debug.WriteLine("Password verified");

                // Small delay to ensure popup is fully dismissed
                await Task.Delay(300);

                // Ask for new email using DisplayPromptAsync instead of custom popup
                System.Diagnostics.Debug.WriteLine("Prompting for new email");
                var newEmail = await Shell.Current.DisplayPromptAsync(
                    "Change Email",
                    "Enter your new email address:",
                    "Save",
                    "Cancel",
                    placeholder: "user@example.com",
                    keyboard: Keyboard.Email
                );

                System.Diagnostics.Debug.WriteLine($"New email received: '{newEmail}'");

                if (string.IsNullOrWhiteSpace(newEmail) || newEmail == user.Email)
                {
                    System.Diagnostics.Debug.WriteLine("Email unchanged or cancelled");
                    return;
                }

                var oldEmail = user.Email;

                System.Diagnostics.Debug.WriteLine($"Updating email from '{oldEmail}' to '{newEmail}'");

                // Update Server
                var request = new UserRequest
                {
                    Email = newEmail,
                    MfaEnabled = user.MfaEnabled,
                    Vault = user.Vault
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{user.Id}",
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

                await Shell.Current.DisplayAlertAsync(
                    "Success",
                    "Email updated successfully.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating email: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Failed to update email: {ex.Message}",
                    "OK");
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

                // Update server
                System.Diagnostics.Debug.WriteLine("Updating server");
                var request = new UserRequest
                {
                    Email = user.Email,
                    MfaEnabled = user.MfaEnabled,
                    Vault = newVaultModel  // Use the NEW encrypted vault!
                };

                var updatedUser = await _httpService.PatchAsync<UserResponse>(
                    $"https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com/v1/users/{user.Id}",
                    request
                );

                System.Diagnostics.Debug.WriteLine("Server updated successfully");

                // Update local database
                var dbUser = await _vaultService.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.Vault = newVaultModel;
                    await _vaultService.UpdateUserInLocalAsync(dbUser);
                    System.Diagnostics.Debug.WriteLine("Local database updated");
                }

                // Update session
                _vaultSessionService.CurrentUser.Vault = newVaultModel;
                _vaultSessionService.MasterPassword = newPassword;

                System.Diagnostics.Debug.WriteLine("Session updated with new password");

                // Confirmation
                await Shell.Current.DisplayAlertAsync(
                    "Success",
                    "Master password updated successfully.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating master password: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"Failed to update master password: {ex.Message}",
                    "OK");
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
                    await Shell.Current.DisplayAlertAsync(
                        "Incorrect Password",
                        "The password you entered is incorrect. Please try again.",
                        "OK");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("Password verified successfully!");
                System.Diagnostics.Debug.WriteLine("===== AUTHORIZATION SUCCESS =====");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authorization failed with exception: {ex}");
                await Shell.Current.DisplayAlertAsync(
                    "Error",
                    $"An error occurred during authorization: {ex.Message}",
                    "OK");
                return false;
            }
        }


        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                IsBusy = true;

                var confirm = await Shell.Current.DisplayAlertAsync(
                    "Logout",
                    "Are you sure you want to logout? All changes will be saved.",
                    "Logout",
                    "Cancel");

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
                await Shell.Current.DisplayAlertAsync("Error", "Failed to logout properly. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    // private void CalculateVaultScore()
    // {
    //     VaultScore = VaultScoreCalculatorService.CalculateScore();
    // }
}