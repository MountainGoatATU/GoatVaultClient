using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

// TODO: Unused UserService injection
public partial class LoginPageViewModel(
    IConfiguration configuration,
    HttpService httpService,
    AuthTokenService authTokenService,
    VaultService vaultService,
    VaultSessionService vaultSessionService,
    ConnectivityService connectivityService)
    : BaseViewModel
{
    // Dependencies

    // Observable Properties
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _isConnected = true;
    [ObservableProperty] private string _connectivityMessage = string.Empty;
    [ObservableProperty] private string _connectionType = string.Empty;
    [ObservableProperty] private ObservableCollection<DbModel> _localAccounts = [];
    [ObservableProperty] private DbModel? _selectedAccount;
    [ObservableProperty] private bool _hasLocalAccounts;
    [ObservableProperty] private string? _offlinePassword;

    public async void Initialize()
    {
        try
        {
            // Get initial state
            UpdateConnectivityState();

            // Subscribe to changes
            connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Load local accounts
            await LoadLocalAccountsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex}");
            // Set defaults in case of error
            HasLocalAccounts = false;
            LocalAccounts.Clear();
        }
    }

    public void Cleanup()
    {
        connectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        UpdateConnectivityState();

        if (!isConnected)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Connection Lost",
                    body: "Your internet connection was lost.",
                    aText: "OK"
                ));
            });
        }
    }

    private void UpdateConnectivityState()
    {
        var networkInfo = connectivityService.GetNetworkInfo();
        IsConnected = networkInfo.IsConnected;
        ConnectionType = networkInfo.GetConnectionType();

        ConnectivityMessage = IsConnected
            ? $"Connected via {ConnectionType}"
            : "No internet connection available";
    }

    private async Task<List<DbModel>> GetAllLocalAccountAsync()
    {
        // Retrieve all local accounts from the vault service
        var dbUsers = await vaultService.LoadAllUsersFromLocalAsync();
        return dbUsers;
    }

    private async Task LoadLocalAccountsAsync()
    {
        // Load local accounts from the vault service
        var accounts = await GetAllLocalAccountAsync();

        // Update the ObservableCollection
        LocalAccounts.Clear();

        // Add accounts to the ObservableCollection
        foreach (var account in accounts)
        {
            LocalAccounts.Add(account);
        }

        // Update HasLocalAccounts property
        HasLocalAccounts = LocalAccounts.Count > 0;
    }

    [RelayCommand]
    private async Task Login()
    {
        // Use GetSection and check for null or empty value to avoid CS8600
        var urlSection = configuration.GetSection("GOATVAULT_SERVER_BASE_URL");
        var url = urlSection.Value;

        if (string.IsNullOrWhiteSpace(url))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Configuration Error",
                body: "Server base URL is not configured.",
                aText: "OK"
            ));
            return;
        }

        // Prevent multiple simultaneous logins
        if (IsBusy)
            return;

        // Check connectivity
        if (!IsConnected)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "No Connection",
                body: "Please check your internet connection and try again.",
                aText: "OK"
            ));
            return;
        }

        // Validation
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "Email and password are required.",
                aText: "OK"
            ));
            return;
        }

        try
        {
            // Set Busy
            IsBusy = true;

            // Double-check connectivity before network call
            var hasConnection = await connectivityService.CheckConnectivityAsync();
            if (!hasConnection)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Connection Error",
                    body: "Unable to verify internet connection.",
                    aText: "OK"
                ));
                return;
            }

            // 2. Init Auth
            var initPayload = new AuthInitRequest { Email = Email };
            var initResponse = await httpService.PostAsync<AuthInitResponse>(
                $"{url}v1/auth/init",
                initPayload
            );

            // 3. Generate Verifier
            System.Diagnostics.Debug.WriteLine("Step 2: Generating auth verifier");
            var loginVerifier = CryptoService.GenerateAuthVerifier(Password, initResponse.AuthSalt);

            // 4. Check if MFA is required
            string? mfaCode = null;
            if (initResponse.MfaEnabled)
            {
                System.Diagnostics.Debug.WriteLine("MFA is enabled, prompting for code");

                // Prompt user for MFA code using custom popup
                var mfaPopup = new AuthorizePopup("Enter your 6-digit authenticator code", isPassword: false);
                await MopupService.Instance.PushAsync(mfaPopup);
                mfaCode = await mfaPopup.WaitForScan();

                if (string.IsNullOrWhiteSpace(mfaCode))
                {
                    System.Diagnostics.Debug.WriteLine("MFA code entry cancelled");
                    return;
                }
            }

            // 5. Verify with MFA code if required
            System.Diagnostics.Debug.WriteLine("Step 3: Calling auth/verify");
            var verifyPayload = new AuthVerifyRequest
            {
                UserId = Guid.Parse(initResponse.UserId),
                AuthVerifier = loginVerifier,
                MfaCode = mfaCode // Will be null if MFA not enabled
            };

            var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                $"{url}v1/auth/verify",
                verifyPayload
            );
            System.Diagnostics.Debug.WriteLine("Auth verify successful");

            authTokenService.SetToken(verifyResponse.AccessToken);
            vaultSessionService.MasterPassword = Password;

            // 6. Get User Data
            System.Diagnostics.Debug.WriteLine("Step 4: Fetching user data");
            var userResponse = await httpService.GetAsync<UserResponse>(
                $"{url}v1/users/{initResponse.UserId}"
            );
            System.Diagnostics.Debug.WriteLine("User data fetched successfully");

            vaultSessionService.CurrentUser = userResponse;

            // 7. Sync Local DB (Delete old if exists, save new)
            System.Diagnostics.Debug.WriteLine("Step 5: Syncing local database");
            var existingUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

            if (existingUser != null)
            {
                System.Diagnostics.Debug.WriteLine("Deleting existing local user");
                await vaultService.DeleteUserFromLocalAsync(vaultSessionService.CurrentUser.Id);
            }

            await vaultService.SaveUserToLocalAsync(new DbModel
            {
                Id = vaultSessionService.CurrentUser.Id,
                Email = vaultSessionService.CurrentUser.Email,
                AuthSalt = vaultSessionService.CurrentUser.AuthSalt,
                MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
                MfaSecret = vaultSessionService.CurrentUser.MfaSecret,
                Vault = vaultSessionService.CurrentUser.Vault,
                CreatedAt = userResponse.CreatedAt,
                UpdatedAt = userResponse.UpdatedAt
            });
            System.Diagnostics.Debug.WriteLine("Local database synced");

            // 8. Decrypt & Store Session
            System.Diagnostics.Debug.WriteLine("Step 6: Decrypting vault");
            vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, Password);
            System.Diagnostics.Debug.WriteLine("Vault decrypted successfully");

            // 9. Navigate to App (MainPage)
            if (Application.Current != null)
                Application.Current.MainPage = new AppShell();

            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Login Failed",
                body: "Invalid email or password. Please try again.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Account Not Found",
                body: "No account found with this email address.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Invalid Request",
                body: "Please check your email and password.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "This email is already registered.",
                aText: "OK"
            ));
        }
        catch (HttpRequestException httpEx)
        {
            // Other HTTP errors - show the actual error message from server
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Connection Error",
                body: $"Unable to connect to the server. {httpEx.Message}",
                aText: "OK"
            ));
        }
        catch (TimeoutException)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Timeout",
                body: "The request timed out. Please check your internet connection and try again.",
                aText: "OK"
            ));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("decrypt"))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Decryption Error",
                body: "Unable to decrypt your vault. This may indicate a data corruption issue.",
                aText: "OK"
            ));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "An unexpected error occurred. Please try again later.",
                aText: "OK"
            ));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Offline Login Command
    [RelayCommand]
    private async Task LoginOffline()
    {
        // Prevent multiple simultaneous logins
        if (IsBusy)
            return;

        // Validation
        if (SelectedAccount == null)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "Please select an account.",
                aText: "OK"
            ));
            return;
        }

        if (string.IsNullOrWhiteSpace(OfflinePassword))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: "Password is required.",
                aText: "OK"
            ));
            return;
        }

        try
        {
            // Set Busy
            IsBusy = true;

            // 1. Load the user from local database
            var dbUser = await vaultService.LoadUserFromLocalAsync(SelectedAccount.Id);

            if (dbUser == null)
            {
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "Account not found in local storage.",
                    aText: "OK"
                ));
                return;
            }

            // 2. Verify the password locally by generating the auth verifier
            var loginVerifier = CryptoService.GenerateAuthVerifier(OfflinePassword, dbUser.AuthSalt);

            // 3. Try to decrypt the vault - if this succeeds, password is correct
            try
            {
                var decryptedVault = vaultService.DecryptVault(dbUser.Vault, OfflinePassword);

                // Password is correct!
                vaultSessionService.MasterPassword = OfflinePassword;
                vaultSessionService.DecryptedVault = decryptedVault;

                // Set current user in session (convert DbModel to UserResponse format)
                vaultSessionService.CurrentUser = new UserResponse
                {
                    Id = dbUser.Id,
                    Email = dbUser.Email,
                    AuthSalt = dbUser.AuthSalt,
                    MfaEnabled = dbUser.MfaEnabled,
                    MfaSecret = dbUser.MfaSecret,
                    Vault = dbUser.Vault,
                    CreatedAt = dbUser.CreatedAt,
                    UpdatedAt = dbUser.UpdatedAt
                };

                // Navigate to app
                await NavigateToMainPage();
            }
            catch
            {
                // Decryption failed - wrong password
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "Incorrect password for this account.",
                    aText: "OK"
                ));
            }
        }
        catch (Exception ex)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "Error",
                body: ex.Message,
                aText: "OK"
            ));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Quick login by selecting a local account (online mode)
    /// This will autofill the email field
    /// </summary>
    [RelayCommand]
    private void SelectLocalAccount(DbModel account)
    {
        if (!IsConnected)
        {
            // In offline mode, use offline login
            SelectedAccount = account;
        }
        else
        {
            // In online mode, auto-fill email
            Email = account.Email;
            SelectedAccount = null; // Clear selection for online login
        }
    }

    [RelayCommand]
    private async Task RemoveOfflineAccount(DbModel? account)
    {
        if (account == null) return;

        // Remove from local database
        await vaultService.DeleteUserFromLocalAsync(account.Id);

        // Remove from ObservableCollection
        LocalAccounts.Remove(account);
    }

    private async Task NavigateToMainPage()
    {
        if (Application.Current != null)
            Application.Current.MainPage = new AppShell();

        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        if (!IsConnected)
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                title: "No Connection",
                body: "Registration requires an internet connection.",
                aText: "OK"
            ));
            return;
        }
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    /// <summary>
    /// Refresh local accounts list
    /// </summary>
    [RelayCommand]
    private async Task RefreshLocalAccounts() => await LoadLocalAccountsAsync();
}
