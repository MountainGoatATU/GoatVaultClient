using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Services;
using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Collections.ObjectModel;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

// TODO: Refactor
public partial class LoginPageViewModel(
    ISyncingService syncing,
    ConnectivityService connectivity,
    LoginOnlineUseCase loginOnline,
    LoginOfflineUseCase loginOffline,
    IUserRepository userRepository,
    ILogger<LoginPageViewModel>? logger = null)
    : BaseViewModel
{
    [ObservableProperty] private string? _emailText;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _isOnline = true;
    [ObservableProperty] private string _connectivityMessage = string.Empty;
    [ObservableProperty] private string _connectionType = string.Empty;
    [ObservableProperty] private ObservableCollection<User> _localUsers = [];
    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private bool _hasLocalUsers;
    [ObservableProperty] private string? _offlinePassword;

    public async Task InitializeAsync()
    {
        UpdateConnectivityState();
        connectivity.ConnectivityChanged += OnConnectivityChanged;
        await LoadLocalUsersAsync();
    }

    #region Connectivity

    public void Cleanup() => connectivity.ConnectivityChanged -= OnConnectivityChanged;

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
        var networkInfo = connectivity.GetNetworkInfo();
        IsOnline = networkInfo.IsConnected;
        ConnectionType = networkInfo.GetConnectionType();
        ConnectivityMessage = IsOnline ? $"Connected via {ConnectionType}" : "No internet connection available";
    }

    #endregion

    private async Task LoadLocalUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        LocalUsers = new ObservableCollection<User>(users);
        HasLocalUsers = LocalUsers.Count > 0;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            if (IsOnline)
            {
                await LoginOnline();
                syncing.StartPeriodicSync(TimeSpan.FromMinutes(2)); // Start auto-sync
            }
            else
            {
                await LoginOffline();
                // We might want to sync if connection comes back? 
                // For now, only start periodic sync if online login, as it implies we have fresh token?
                // Actually LoginOffline doesn't get a token usually unless we refresh it.
            }

            // Navigate to main
            await Shell.Current.GoToAsync("//main/home");
        }
        catch (UnauthorizedAccessException)
        {
            await Shell.Current.DisplayAlertAsync("Login Failed", "Invalid credentials or MFA.", "OK");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Unexpected error during login");
            await Shell.Current.DisplayAlertAsync("Error", e.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoginOnline()
    {
        if (string.IsNullOrWhiteSpace(EmailText))
            throw new InvalidOperationException("Email is required.");
        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("Password is required.");

        await loginOnline.ExecuteAsync(new Email(EmailText), Password, PromptForMfaCode);
    }

    private async Task LoginOffline()
    {
        if (SelectedUser == null)
            throw new InvalidOperationException("Select a local account for offline login.");

        await loginOffline.ExecuteAsync(SelectedUser.Id, OfflinePassword!);
    }

    private static async Task<string?> PromptForMfaCode()
    {
        var mfaPopup = new AuthorizePopup("Enter 6-digit MFA code", isPassword: false);
        await MopupService.Instance.PushAsync(mfaPopup);
        return await mfaPopup.WaitForScan();
    }

    [RelayCommand]
    private void SelectLocalUser(User user)
    {
        if (!IsOnline)
        {
            SelectedUser = user;
        }
        else
        {
            EmailText = user.Email.Value;
            SelectedUser = null; // Clear selection for online login
        }
    }

    [RelayCommand]
    private async Task RemoveOfflineUser(User user)
    {
        var confirm = await Shell.Current.DisplayAlertAsync("Remove User",
            $"Are you sure you want to remove {user.Email} from this device?", "Yes", "No");

        if (!confirm) return;

        await userRepository.DeleteAsync(user);
        await LoadLocalUsersAsync();
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        if (!IsOnline)
        {
            await Shell.Current.DisplayAlertAsync("No Connection", "Registration requires an internet connection.", "OK");
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

    [RelayCommand]
    private async Task RefreshLocalUsers() => await LoadLocalUsersAsync();

    [RelayCommand]
    private void ClearSelection() => SelectedUser = null;
}
