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
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

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

    public bool ShowInvalidEmail =>
        !string.IsNullOrWhiteSpace(Email) && !IsEmailValid;

    public bool ShowPasswordRequirements =>
        !string.IsNullOrEmpty(Password);

    public bool ShowPasswordMismatch =>
           !string.IsNullOrEmpty(ConfirmPassword) &&
           Password != ConfirmPassword;

    public bool AllValid =>
           IsLengthValid &&
           HasUpperCase &&
           HasLowerCase &&
           HasDigit &&
           HasSpecialChar &&
           IsEmailValid &&
           !ShowPasswordMismatch;

    partial void OnPasswordChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            IsLengthValid = HasUpperCase = HasLowerCase = HasDigit = HasSpecialChar = false;
            return;
        }

        else
        {
            IsLengthValid = value.Length >= 12;
            HasUpperCase = value.Any(char.IsUpper);
            HasLowerCase = value.Any(char.IsLower);
            HasDigit = value.Any(char.IsDigit);
            HasSpecialChar = value.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c));
        }

        NotifyValidationChanged();
    }

    partial void OnConfirmPasswordChanged(string? value)
    {
        NotifyValidationChanged();
    }

    partial void OnEmailChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            IsEmailValid = false;
            return;
        }

        else
        {
            IsEmailValid = EmailRegex.IsMatch(value);
        }

        NotifyValidationChanged();
    }
    private void NotifyValidationChanged()
    {
        OnPropertyChanged(nameof(ShowPasswordRequirements));
        OnPropertyChanged(nameof(ShowPasswordMismatch));
        OnPropertyChanged(nameof(ShowInvalidEmail));
        OnPropertyChanged(nameof(AllValid));
        RegisterCommand.NotifyCanExecuteChanged();
    }

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
