using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Pages;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

public partial class RegisterPageViewModel(RegisterUseCase register) : BaseViewModel
{
    // Services
    private CancellationTokenSource? _debounceCts;

    // Observable Properties (Bound to Entry fields)
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string? _confirmPassword;
    [ObservableProperty] private bool _createRecovery;

    [ObservableProperty] private string _passwordMessage = string.Empty;
    [ObservableProperty] private bool _isPasswordWarning;

    [ObservableProperty] private string _confirmPasswordMessage = string.Empty;
    [ObservableProperty] private bool _isConfirmPasswordWarning;

    [ObservableProperty] private string _emailMessage = string.Empty;
    [ObservableProperty] private bool _isEmailWarning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private bool _isPasswordGood;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private bool _isConfirmPasswordGood;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private bool _isEmailGood;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private bool _isFormViewValid;

    public bool IsFormValid => 
        IsEmailGood &&
        IsPasswordGood &&
        IsConfirmPasswordGood;

    partial void OnEmailChanged(string? value)
    {
        var result = register.ValidateEmail(Email);
        IsEmailWarning = result.IsWarning;
        IsEmailGood = result.IsGood;
        EmailMessage = result.Message;
    }
    partial void OnPasswordChanged(string? value)
    {
        // Cancel previous pending API call if user is typing fast
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        _ = ValidateInputAsync(value, _debounceCts.Token);

        ValidatePasswordMatch();
    }

    // NEW: Trigger the match check when the Confirm Password field changes
    partial void OnConfirmPasswordChanged(string? value) => ValidatePasswordMatch();

    // NEW: Synchronous logic to evaluate password match
    private void ValidatePasswordMatch()
    {
        // Don't show errors if they haven't started typing in the confirmation box yet
        if (string.IsNullOrEmpty(ConfirmPassword))
        {
            IsConfirmPasswordWarning = false;
            IsConfirmPasswordGood = false;
            ConfirmPasswordMessage = string.Empty;
            return;
        }

        if (Password != ConfirmPassword)
        {
            IsConfirmPasswordWarning = true;
            IsConfirmPasswordGood = false;
            ConfirmPasswordMessage = "Passwords do not match.";
        }
        else
        {
            IsConfirmPasswordWarning = false;
            IsConfirmPasswordGood = true;
            ConfirmPasswordMessage = "Passwords match.";
        }
    }

    private async Task ValidateInputAsync(string? value, CancellationToken token)
    {
        // Wait 500ms before hitting the Use Case to avoid spamming the API
        try { await Task.Delay(300, token); }
        catch (TaskCanceledException) { return; }

        if (token.IsCancellationRequested) 
            return;

        // Delegate business logic to the Use Case
        var passwordValidationResult = await register.ValidatePasswordAsync(value);

        if (!token.IsCancellationRequested)
        {
            IsPasswordWarning = passwordValidationResult.IsWarning;
            IsPasswordGood = passwordValidationResult.IsGood;
            PasswordMessage = passwordValidationResult.Message;
        }
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

            if (CreateRecovery)
                await Shell.Current.GoToAsync($"{ nameof(SplitSecretPage)}?Mp={Password}");
            else
                await Shell.Current.GoToAsync("//gratitude");
        });
    }

    [RelayCommand]
    private static async Task GoToLogin() => await Shell.Current.GoToAsync("//login");
}

