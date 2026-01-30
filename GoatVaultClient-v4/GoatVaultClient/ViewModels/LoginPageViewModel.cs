using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultClient.Services.Vault;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;

namespace GoatVaultClient.ViewModels;

// TODO: Unused UserService injection
public partial class LoginPageViewModel(
    /*UserService userService,*/
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

    public void InitializeConnection()
    {
        // Get initial state
        UpdateConnectivityState();
        // Subscribe to changes
        connectivityService.ConnectivityChanged += OnConnectivityChanged;
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
                await Shell.Current.DisplayAlertAsync(
                    "Connection Lost",
                    "Your internet connection was lost.",
                    "OK");
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

    [RelayCommand]
    private async Task Login()
    {
        const string url = "https://y9ok4f5yja.execute-api.eu-west-1.amazonaws.com";
        
        // Prevent multiple simultaneous logins
        if (IsBusy)
            return;

        // Check connectivity
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlertAsync("No Connection", "Please check your internet connection and try again.", "OK");
            return;
        }
        // Validation
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Email and password are required.", "OK");
            return;
        }

        try
        {
            // Set Busy
            IsBusy = true;

            //Double-check connectivity before network call
            var hasConnection = await connectivityService.CheckConnectivityAsync();
            if (!hasConnection) 
            {
                await Shell.Current.DisplayAlertAsync(
                    "Connection Error",
                    "Unable to verify internet connection.",
                    "OK");
                return;
            }

            // 2. Init Auth
            var initPayload = new AuthInitRequest { Email = Email };
            var initResponse = await httpService.PostAsync<AuthInitResponse>(
                $"{url}/v1/auth/init",
                initPayload
            );

            // 3. Generate Verifier
            var loginVerifier = CryptoService.GenerateAuthVerifier(Password, initResponse.AuthSalt);

            // 4. Verify
            var verifyPayload = new AuthVerifyRequest
            {
                UserId = Guid.Parse(initResponse.UserId),
                AuthVerifier = loginVerifier
            };
            var verifyResponse = await httpService.PostAsync<AuthVerifyResponse>(
                $"{url}/v1/auth/verify",
                verifyPayload
            );
            authTokenService.SetToken(verifyResponse.AccessToken);
            vaultSessionService.MasterPassword = Password;

            // 5. Get User Data
            var userResponse = await httpService.GetAsync<UserResponse>(
                $"{url}/v1/users/{initResponse.UserId}"
            );

            vaultSessionService.CurrentUser = userResponse;
            // 6. Sync Local DB (Delete old if exists, save new)
            var existingUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

            if (existingUser != null)
                await vaultService.DeleteUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

            await vaultService.SaveUserToLocalAsync(new DbModel
            {
                Id = vaultSessionService.CurrentUser.Id,
                Email = vaultSessionService.CurrentUser.Email,
                AuthSalt = vaultSessionService.CurrentUser.AuthSalt,
                MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
                Vault = vaultSessionService.CurrentUser.Vault
            });

            // 7. Decrypt & Store Session
            vaultSessionService.DecryptedVault = vaultService.DecryptVault(vaultSessionService.CurrentUser.Vault, Password);

            // 8. Navigate to App (MainPage)
            // Note: Originally you went to GratitudePage, but for login, MainPage is standard.
            // Using "//MainPage" clears the stack so 'Back' doesn't go to Login.

            if (Application.Current != null)
                Application.Current.MainPage = new AppShell();

            await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
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
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static async Task GoToRegister()
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}