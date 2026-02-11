using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Services;
using GoatVaultCore.Models;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Mopups.Services;
using GoatVaultClient.Controls.Popups;

namespace GoatVaultClient.ViewModels;

// TODO: Unused UserService injection
public partial class LoginPageViewModel(
    IAuthenticationService authenticationService,
    ISyncingService syncingService,
    VaultService vaultService,
    ConnectivityService connectivityService,
    ILogger<LoginPageViewModel>? logger = null)
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

    public async Task InitializeAsync()
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
        catch (Exception e)
        {
            logger?.LogError(e, "Error initializing LoginPageViewModel");
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
                try
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Connection Lost",
                        "Your internet connection was lost.",
                        "OK");
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error displaying connectivity alert");
                }
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

    private async Task LoadLocalAccountsAsync()
    {
        LocalAccounts.Clear();
        LocalAccounts = await authenticationService.GetAllLocalAccountsAsync()
            .ContinueWith(t => new ObservableCollection<DbModel>(t.Result));
        if (LocalAccounts.Count != 0)
        {
            HasLocalAccounts = true;
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        // Prevent multiple simultaneous logins
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            // Define the MFA provider function
            async Task<string?> MfaProvider()
            {
                logger?.LogDebug("MFA enabled, prompting user for code");
                string? mfaCode = null;

                // Need to invoke on main thread as this is UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var mfaPopup = new AuthorizePopup("Enter your 6-digit authenticator code", isPassword: false);
                    await MopupService.Instance.PushAsync(mfaPopup);
                    mfaCode = await mfaPopup.WaitForScan();
                });

                return mfaCode;
            }

            // Call the service
            var success = await authenticationService.LoginAsync(Email, Password, MfaProvider);

            if (success)
            {
                // Navigate to vault
                await Shell.Current.GoToAsync("//main/home");
            }
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
        try
        {
            // Set Busy
            IsBusy = true;

            // Attempt Login offline
            var result = await authenticationService.LoginOfflineAsync(Email, OfflinePassword, SelectedAccount);
            if (result)
            {
                // Navigate to Main page
                await Shell.Current.GoToAsync("//main/home");
            }
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
    private async Task RemoveOfflineAccount(DbModel account)
    {
        // Remove from service
        await authenticationService.RemoveLocalAccountAsync(account);
        // Refresh list
        await LoadLocalAccountsAsync();
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        if (!IsConnected)
        {
            await Shell.Current.DisplayAlertAsync(
                "No Connection",
                "Registration requires an internet connection.",
                "OK");
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("//register");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error navigating to register page");
        }
    }

    /// <summary>
    /// Refresh local accounts list
    /// </summary>
    [RelayCommand]
    private async Task RefreshLocalAccounts()
    {
        await LoadLocalAccountsAsync();
    }

    /// <summary>
    /// Clear offline account selection (cancel offline login)
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        SelectedAccount = null;
    }
}
