using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Pages;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

public partial class RegisterPageViewModel(
    RegisterUseCase register)
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
        await SafeExecuteAsync(async () =>
        {
            // Call Register use case
            if (Email is not null && Password is not null)
                await register.ExecuteAsync(new Email(Email), Password);

            // On success, navigate to Gratitude page
            await Shell.Current.GoToAsync("//gratitude");
        });
    }

    [RelayCommand]
    private static async Task GoToLogin() =>
        // Navigate back to Login
        await Shell.Current.GoToAsync("//login");
    [RelayCommand]
    private static async Task GoToRecover() =>
        // Navigate back to Login
        await Shell.Current.GoToAsync(nameof(RecoverSecretPage));
}
