using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultClient.Pages;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using GoatVaultClient.Services;

namespace GoatVaultClient.ViewModels;

public partial class RegisterPageViewModel(
    IAuthenticationService authenticationService)
    : BaseViewModel
{
    // Services

    // Observable Properties (Bound to Entry fields)
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string? _confirmPassword;

    // Constructor (Clean Dependency Injection)

    [RelayCommand]
    private async Task Register()
    {
        // Prevent multiple registrations
        if (IsBusy)
            return;

        try
        {
            // Set Busy
            IsBusy = true;
            // Call Register method from AuthenticationService
            await authenticationService.RegisterAsync(Email, Password, ConfirmPassword);
            // On success, navigate to Gratitude page
            await Shell.Current.GoToAsync(nameof(GratitudePage));
        }
        finally
        {
            IsBusy = false;
           
        }
    }

    [RelayCommand]
    private static async Task GoToLogin()
    {
        // Navigate back to Login
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}
