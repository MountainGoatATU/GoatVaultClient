using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Pages;
using GoatVaultClient.Services;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

// TODO: Unused UserService injection
public partial class LoginPageViewModel(
    IAuthenticationService authenticationService,
    VaultService vaultService,
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
    [ObservableProperty] private ObservableCollection<DbModel> _localAccounts = new();
    [ObservableProperty] private DbModel? _selectedAccount;
    [ObservableProperty] private bool _hasLocalAccounts;
    [ObservableProperty] private string? _offlinePassword;
    public async void Initialize()
    {
        // Get initial state
        UpdateConnectivityState();
        // Subscribe to changes
        connectivityService.ConnectivityChanged += OnConnectivityChanged;
        // Load local accounts
        await LoadLocalAccountsAsync();
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
    private async Task LoadLocalAccountsAsync()
    {
        LocalAccounts.Clear();
        LocalAccounts = await authenticationService.GetAllLocalAccountsAsync()
            .ContinueWith(t => new ObservableCollection<DbModel>(t.Result));
        if (LocalAccounts.Count() != 0)
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
            // Set Busy
            IsBusy = true;
            // 1. Attempt online login
            await authenticationService.LoginAsync(Email, Password);
            // 2. On success, navigate to main page
            await NavigateToMainPage();
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
            await authenticationService.LoginOfflineAsync(Email, OfflinePassword, SelectedAccount);
            // Navigate to app
            await NavigateToMainPage();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Quick login by selecting a local account (online mode)
    /// This will auto-fill the email field
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
            await Shell.Current.DisplayAlertAsync(
                "No Connection",
                "Registration requires an internet connection.",
                "OK");
            return;
        }
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    /// <summary>
    /// Refresh local accounts list
    /// </summary>
    [RelayCommand]
    private async Task RefreshLocalAccounts()
    {
        await LoadLocalAccountsAsync();
    }
}