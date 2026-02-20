using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using GoatVaultClient.Pages;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

public partial class RegisterPageViewModel : BaseViewModel
{
    // Services
    private readonly RegisterUseCase _register;
    private CancellationTokenSource? _debounceCts;
    // Observable Properties (Bound to Entry fields)
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? password;
    [ObservableProperty] private string? confirmPassword;
    [ObservableProperty] private bool createRecovery = false;

    [ObservableProperty] private string passwordMessage = string.Empty;
    [ObservableProperty] private bool isPasswordWarning;
    [ObservableProperty] private bool isPasswordGood;

    [ObservableProperty] private string confirmPasswordMessage = string.Empty;
    [ObservableProperty] private bool isConfirmPasswordWarning;
    [ObservableProperty] private bool isConfirmPasswordGood;

    // Constructor (Clean Dependency Injection)
    public RegisterPageViewModel(RegisterUseCase register)
    {
        _register = register;
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
    partial void OnConfirmPasswordChanged(string? value)
    {
        ValidatePasswordMatch();
    }

    // NEW: Synchronous logic to evaluate password match
    private void ValidatePasswordMatch()
    {
        // Don't show errors if they haven't started typing in the confirm box yet
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
        try { await Task.Delay(500, token); }
        catch (TaskCanceledException) { return; }

        if (token.IsCancellationRequested) return;

        // Delegate business logic to the Use Case
        var validationResult = await _register.ValidatePasswordAsync(value);

        if (!token.IsCancellationRequested)
        {
            IsPasswordWarning = validationResult.IsWarning;
            IsPasswordGood = validationResult.IsGood;
            PasswordMessage = validationResult.Message;
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
                await _register.ExecuteAsync(new Email(Email), Password);

            if (CreateRecovery)
            {
                await Shell.Current.GoToAsync($"{ nameof(SplitSecretPage)}?Mp={Password}");
            }
            else
            {
                await Shell.Current.GoToAsync("//gratitude");
            }
            

        });
    }

    [RelayCommand]
    private static async Task GoToLogin() => await Shell.Current.GoToAsync("//login");
}

