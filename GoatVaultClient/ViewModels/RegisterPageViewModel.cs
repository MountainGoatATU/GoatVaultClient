using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Pages;
using System.Text.RegularExpressions;
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

    // Validation Properties (Bound to UI indicators)
    [ObservableProperty] private bool _isLengthValid;
    [ObservableProperty] private bool _hasUpperCase;
    [ObservableProperty] private bool _hasLowerCase;
    [ObservableProperty] private bool _hasDigit;
    [ObservableProperty] private bool _hasSpecialChar;
    [ObservableProperty] private bool _isEmailValid;

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

    partial void OnPasswordChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            IsLengthValid = HasUpperCase = HasLowerCase = HasDigit = HasSpecialChar = false;
            return;
        }

        IsLengthValid = value.Length >= 12;
        HasUpperCase = value.Any(char.IsUpper);
        HasLowerCase = value.Any(char.IsLower);
        HasDigit = value.Any(char.IsDigit);
        HasSpecialChar = value.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c));
    }

    partial void OnEmailChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            IsEmailValid = false;
            return;
        }

        var regex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        IsEmailValid = regex.IsMatch(value);
    }

    public bool AllValid =>
        IsLengthValid &&
        HasUpperCase &&
        HasLowerCase &&
        HasDigit &&
        HasSpecialChar &&
        IsEmailValid &&
        Password == ConfirmPassword;
}
