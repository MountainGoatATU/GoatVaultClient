using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Services;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Services.API;
using Mopups.Services;

namespace GoatVaultClient.ViewModels
{
    // TODO: Refactor
    public partial class UserPageViewModel : BaseViewModel
    {
        [ObservableProperty] private string email;
        [ObservableProperty] private bool mfaEnabled;
        [ObservableProperty] private string? mfaSecret;
        [ObservableProperty] private string? mfaQrCodeUrl;

        [ObservableProperty] private double vaultScore;
        [ObservableProperty] private string? breachesText;
        [ObservableProperty] private string? mfaStatusText;
        [ObservableProperty] private string? reuseRateText;
        [ObservableProperty] private string? masterPasswordStrength;
        [ObservableProperty] private string? averagePasswordsStrength;
        [ObservableProperty] private bool goatEnabled = true;

        [ObservableProperty] private bool showVaultDetails;
        [ObservableProperty] private string? vaultTierText;

        private readonly IHttpService _http;
        private readonly IAuthTokenService _authToken;
        private readonly IAuthenticationService _auth;
        private readonly ISessionContext _session;
        private readonly IVaultCrypto _vaultCrypto;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly GoatTipsService _goatTips;
        private readonly PwnedPasswordService _pwned;

        public UserPageViewModel(
            IHttpService http,
            IAuthTokenService authToken,
            IAuthenticationService auth,
            ISessionContext session,
            IVaultCrypto vaultCrypto,
            Microsoft.Extensions.Configuration.IConfiguration config,
            GoatTipsService goatTips,
            PwnedPasswordService pwned)
        {
            _http = http;
            _authToken = authToken;
            _auth = auth;
            _session = session;
            _vaultCrypto = vaultCrypto;
            _config = config;
            _goatTips = goatTips;
            _pwned = pwned;

            // TODO: Fix
            // _session.VaultEntriesChanged += RefreshVaultScore;
            // _session.MasterPasswordChanged += RefreshVaultScore;

            GoatEnabled = _goatTips.IsGoatEnabled;
            _goatTips.SetEnabled(GoatEnabled);

            // if (_session.CurrentUser == null)
            //     return;

            // Email = _session.CurrentUser.Email;
            // MfaEnabled = _session.CurrentUser.MfaEnabled;

            RefreshVaultScore();
        }

        [RelayCommand]
        public void RefreshVaultScore()
        {
            // TODO: Fix
            /*
            var user = _session.CurrentUser;
            if (user == null)
                return;

            var score = VaultScoreCalculatorService.CalculateScore(
                _session.VaultEntries,
                _session.MasterPassword,
                user.MfaEnabled);

            VaultScore = score.VaultScore;
            MasterPasswordStrength = $"{score.MasterPasswordPercent}%";
            AveragePasswordsStrength = $"{score.AveragePasswordsPercent}%";
            ReuseRateText = $"{score.ReuseRatePercent}%";
            BreachesText = $"{score.BreachesCount}";
            MfaStatusText = score.MfaEnabled ? "Enabled" : "Disabled";
            VaultTierText = GetVaultTier(VaultScore);
            */
        }

        [RelayCommand]
        private void ToggleVaultDetails()
        {
            ShowVaultDetails = !ShowVaultDetails;
        }

        private static string GetVaultTier(double score)
        {
            return score switch
            {
                >= 900 => "The Summit Sovereign (900+)",
                >= 750 => "The Ridge Walker (750-899)",
                >= 500 => "The Cliffside Scrambler (500-749)",
                >= 300 => "The Treeline Grazer (300-499)",
                _ => "The Dead Meat (< 300)"
            };
        }

        [RelayCommand]
        private async Task ShowVaultDetailsPopupAsync()
        {
            var message =
                $"Tier: {VaultTierText}\n" +
                $"\nMaster password: {MasterPasswordStrength}" +
                $"\nAverage passwords: {AveragePasswordsStrength}" +
                $"\nOriginality: {ReuseRateText}" +
                $"\nBreached passwords: {BreachesText}" +
                $"\nMFA: {MfaStatusText}";

            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Vault Score Details",
                body: message,
                aText: "OK"
            ));
        }

        [RelayCommand]
        private void ToggleGoat()
        {
            GoatEnabled = !GoatEnabled;
            _goatTips.SetEnabled(GoatEnabled);
        }

        private string GetBaseUrl()
        {
            var baseUrl = _config["GOATVAULT_SERVER_BASE_URL"];
            return string.IsNullOrWhiteSpace(baseUrl) ? throw new Exception("Server base URL not configured") : baseUrl;
        }

        [RelayCommand]
        private async Task EnableMfaAsync()
        {
            if (IsBusy)
                return;

            // TODO: Refactor into use case
            /*
            var user = _session.CurrentUser;
            if (user == null)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "No user logged in.",
                    aText: "OK"
                ));
                return;
            }
            */

            try
            {
                IsBusy = true;

                // Ask for current password to authorize
                var password = await PromptUserAsync("Confirm Password", true);
                if (password == null)
                    return;

                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                    return;

                // Wait for password popup to fully dismiss
                await Task.Delay(300);

                /*
                // Generate new TOTP secret
                var secretBytes = KeyGeneration.GenerateRandomKey(20);
                var secret = Base32Encoding.ToString(secretBytes);
                MfaSecret = secret;

                // Generate QR code URL for authenticator apps
                const string issuer = "GoatVault";
                MfaQrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(Email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

                // Show QR code and secret to user
                await ShowMfaSetupDialog();

                await Task.Delay(300);

                // Ask user to enter a code to verify they've set it up
                var verificationCode = await PromptUserAsync("Enter 6-digit code from authenticator app", false);

                if (string.IsNullOrWhiteSpace(verificationCode))
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Setup Cancelled",
                        body: "MFA setup was cancelled.",
                        aText: "OK"
                    ));
                    return;
                }

                // Verify the code
                var isValid = TotpService.VerifyCode(secret, verificationCode);
                if (!isValid)
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "Invalid Code",
                        body: "The code you entered is incorrect. Please try again.",
                        aText: "OK"
                    ));
                    return;
                }

                var baseUrl = GetBaseUrl();

                // Update server with MFA enabled and secret
                var request = new UserRequest
                {
                    Email = user.Email,
                    MfaEnabled = true,
                    MfaSecret = secret,
                    Vault = user.Vault
                };

                await _http.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                // Update local database
                var dbUser = await _vaultCrypto.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.MfaEnabled = true;
                    dbUser.MfaSecret = secret;
                    await _vaultCrypto.UpdateUserInLocalAsync(dbUser);
                }

                // Update session
                _session.CurrentUser?.MfaEnabled = true;
                _session.CurrentUser?.MfaSecret = secret;
                MfaEnabled = true;
                RefreshVaultScore();
                
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "MFA Enabled",
                    body: "Two-factor authentication has been successfully enabled for your account.",
                    aText: "OK"
                ));
            */
            }
            catch (Exception ex)
            {
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
            if (IsBusy)
                return;

            // TODO: Refactor into use case

            /*
            var user = _session.CurrentUser;
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
            */

            try
            {
                IsBusy = true;

                // Confirm action
                var confirm = false;
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
                string? password = null;
                await MainThread.InvokeOnMainThreadAsync(async () => password = await PromptUserAsync("Confirm Password", true));

                if (password == null)
                {
                    IsBusy = false;
                    return;
                }

                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                {
                    IsBusy = false;
                    return;
                }

                await Task.Delay(300);

                /*

                // Ask for current MFA code to verify
                string? mfaCode = null;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var mfaPopup = new AuthorizePopup("Enter your current 6-digit MFA code", isPassword: false);
                    await MopupService.Instance.PushAsync(mfaPopup);
                    mfaCode = await mfaPopup.WaitForScan();
                });

                if (string.IsNullOrWhiteSpace(mfaCode))
                {
                    IsBusy = false;
                    return;
                }

                // Load MFA secret from local DB to verify the code
                var dbUser = await _vaultCrypto.LoadUserFromLocalAsync(user.Id);
                if (dbUser == null || string.IsNullOrWhiteSpace(dbUser.MfaSecret))
                {
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
                var isValidCode = TotpService.VerifyCode(dbUser.MfaSecret, mfaCode);
                if (!isValidCode)
                {
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

                // Generate auth_verifier from current password for backend
                var authSaltBytes = Convert.FromBase64String(user.AuthSalt);
                var authVerifier = CryptoService.HashPassword(password, authSaltBytes);

                var baseUrl = GetBaseUrl();

                // Update server with MFA disabled
                await _http.PatchAsync<UserResponse>(
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

                // Update local database
                dbUser.MfaEnabled = false;
                dbUser.MfaSecret = null;
                await _vaultCrypto.UpdateUserInLocalAsync(dbUser);

                // Update session
                _session.CurrentUser?.MfaEnabled = false;
                _session.CurrentUser?.MfaSecret = null;
                MfaEnabled = false;
                RefreshVaultScore();

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MopupService.Instance.PushAsync(new PromptPopup(
                        title: "MFA Disabled",
                        body: "Two-factor authentication has been successfully disabled.",
                        aText: "OK"
                    ));
                });
                */
            }
            catch (Exception ex)
            {
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
            var message = $"""
                           Scan this QR code with your authenticator app:
                           
                           Or enter this secret manually:
                           
                           {MfaSecret}
                           
                           After adding to your app, you'll need to enter a code to verify.
                           
                           Recommended apps:
                           - Google Authenticator
                           - Microsoft Authenticator
                           - Authy
                           """;

            var setupPopup = new PromptPopup(
                title: "Setup MFA",
                body: message,
                aText: "OK"
            );
            await MopupService.Instance.PushAsync(setupPopup);

            // Wait for user to dismiss the setup popup before proceeding
            await setupPopup.WaitForScan();

            // Ensure popup is fully dismissed
            while (MopupService.Instance.PopupStack.Contains(setupPopup))
                await Task.Delay(50);

            // Copy secret to clipboard for easy entry
            await Clipboard.Default.SetTextAsync(MfaSecret ?? "");

            var copiedPopup = new PromptPopup(
                title: "Secret Copied",
                body: "The secret has been copied to your clipboard.",
                aText: "OK"
            );
            await MopupService.Instance.PushAsync(copiedPopup);

            // Wait for user to dismiss the copied popup
            await copiedPopup.WaitForScan();
        }

        [RelayCommand]
        private async Task EditEmailAsync()
        {
            // TODO: Refactor into use case
            /*
            var user = _session.CurrentUser;
            if (user == null)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "No user logged in.",
                    aText: "OK"
                ));
                return;
            }
            */

            try
            {
                IsBusy = true;

                var enteredPassword = await PromptUserAsync("Confirm Password", true);
                if (enteredPassword == null)
                    return;

                var authorized = await AuthorizeAsync(enteredPassword);
                if (!authorized)
                    return;

                // Small delay to ensure first popup is fully dismissed
                await Task.Delay(300);

                var newEmail = await PromptUserAsync("Enter new email", false);

                /*

                if (string.IsNullOrWhiteSpace(newEmail) || newEmail == user.Email)
                    return;

                var baseUrl = GetBaseUrl();

                // Update Server
                var request = new UserRequest
                {
                    Email = newEmail,
                    MfaEnabled = user.MfaEnabled,
                    Vault = user.Vault
                };

                var updatedUser = await _http.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                // Update local database
                var dbUser = await _vaultCrypto.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.Email = newEmail;
                    await _vaultCrypto.UpdateUserInLocalAsync(dbUser);
                }

                // Update session and UI
                _session.CurrentUser?.Email = updatedUser.Email;
                Email = updatedUser.Email;

                */

                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Success",
                    body: "Email updated successfully.",
                    aText: "OK"
                ));
            }
            catch (Exception ex)
            {
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
            // TODO: Refactor into use case
            /*
            var user = _session.CurrentUser;
            if (user == null)
                return;
            */
            try
            {
                IsBusy = true;

                var password = await PromptUserAsync("Confirm Password", true);
                if (password == null)
                    return;

                var authorized = await AuthorizeAsync(password);
                if (!authorized)
                    return;

                // Small delay to ensure first popup is fully dismissed
                await Task.Delay(300);

                var newPassword = await PromptUserAsync("Enter new master password", true);

                if (string.IsNullOrWhiteSpace(newPassword))
                    return;

                /*

                // Check if password has been pwned
                var pwnCount = await _pwned.CheckPasswordAsync(newPassword);
                if (pwnCount > 0)
                {
                    var prompt = new PromptPopup(
                        "Breached Password",
                        $"The password you entered was found in breach databases {pwnCount} times. Do you want to continue?",
                        "Continue",
                        "Back"
                    );

                    await MopupService.Instance.PushAsync(prompt);
                    var proceed = await prompt.WaitForScan();
                    if (!proceed)
                    {
                        // user chose not to continue
                        return;
                    }
                }

                // Re-encrypt vault with new password
                var newVaultModel = _vaultCrypto.EncryptVault(newPassword, _session.DecryptedVault);

                // Generate new authentication credentials
                var newAuthSalt = CryptoService.GenerateAuthSalt();
                var newAuthVerifier = CryptoService.HashPassword(newPassword, newAuthSalt);
                var newAuthSaltBase64 = Convert.ToBase64String(newAuthSalt);

                var baseUrl = GetBaseUrl();

                var request = new
                {
                    auth_salt = newAuthSaltBase64,
                    auth_verifier = newAuthVerifier,
                    email = user.Email,
                    mfa_enabled = user.MfaEnabled,
                    mfa_secret = user.MfaEnabled ? _session.CurrentUser?.MfaSecret : null,
                    vault = newVaultModel
                };

                await _http.PatchAsync<UserResponse>(
                    $"{baseUrl}v1/users/{user.Id}",
                    request
                );

                // Update local database
                var dbUser = await _vaultCrypto.LoadUserFromLocalAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.AuthSalt = newAuthSaltBase64;
                    dbUser.Vault = newVaultModel;
                    dbUser.MfaSecret = user.MfaEnabled ? _session.CurrentUser?.MfaSecret : null;
                    await _vaultCrypto.UpdateUserInLocalAsync(dbUser);
                }

                // Update session
                _session.CurrentUser?.AuthSalt = newAuthSaltBase64;
                _session.CurrentUser?.Vault = newVaultModel;
                _session.MasterPassword = newPassword;

                // Recalculate all vault-related scores
                RefreshVaultScore();

                */

                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Success",
                    body: "Master password updated successfully. You can now login with your new password.",
                    aText: "OK"
                ));
            }
            catch (Exception ex)
            {
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

        private static async Task<string?> PromptUserAsync(string title, bool isPassword)
        {
            try
            {
                var popup = new AuthorizePopup(title, isPassword: isPassword);
                await MopupService.Instance.PushAsync(popup);
                var result = await popup.WaitForScan();
                return string.IsNullOrWhiteSpace(result) ? null : result;
            }
            catch
            {
                return null;
            }
        }

        // TODO: Fix
        private async Task<bool> AuthorizeAsync(string? enteredPassword = null)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword))
                return false;

            try
            {
                //if (enteredPassword == _session.MasterPassword)
                if (enteredPassword == "TEMP")
                    return true;

                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Incorrect Password",
                    body: "The password you entered is incorrect. Please try again.",
                    aText: "OK"
                ));
                return false;

            }
            catch (Exception ex)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: $"An error occurred during authorization: {ex.Message}",
                    aText: "OK"
                ));
                return false;
            }
        }

        // TODO: Refactor into use case
        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;
                /*
                await _auth.LogoutAsync();
                */
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
