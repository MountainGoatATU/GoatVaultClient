using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Services;
using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Collections.ObjectModel;
using GoatVaultCore.Models;
using Email = GoatVaultCore.Models.Email;

namespace GoatVaultClient.ViewModels;

// TODO: Unused UserService injection
public partial class LoginPageViewModel(
    ISyncingService syncingService,
    ConnectivityService connectivityService,
    LoginOnlineUseCase loginOnlineUseCase,
    LoginOfflineUseCase loginOfflineUseCase,
    ILogger<LoginPageViewModel>? logger = null)
    : BaseViewModel
{
    [ObservableProperty] private Email? _email;
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
        connectivityService.ConnectivityChanged += OnConnectivityChanged;
        await LoadLocalUsersAsync();
    }

    #region Connectivity

    public void Cleanup() => connectivityService.ConnectivityChanged -= OnConnectivityChanged;

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
        IsOnline = networkInfo.IsConnected;
        ConnectionType = networkInfo.GetConnectionType();
        ConnectivityMessage = IsOnline ? $"Connected via {ConnectionType}" : "No internet connection available";
    }

    #endregion

    private async Task LoadLocalUsersAsync()
    {
        var users = await syncingService.GetAllLocalUsersAsync();
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
                await LoginOnline();
            else
                await LoginOffline();

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
        await loginOnlineUseCase.ExecuteAsync(Email, Password, PromptForMfaCode);
    }

    private async Task LoginOffline()
    {
        if (SelectedUser == null)
            throw new InvalidOperationException("Select a local account for offline login.");

        await loginOfflineUseCase.ExecuteAsync(SelectedUser.Id, OfflinePassword!);
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
            Email = user.Email;
            SelectedUser = null; // Clear selection for online login
        }
    }

    [RelayCommand]
    private async Task RemoveOfflineUser(User user)
    {
        await syncingService.RemoveLocalUserAsync(user);
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
