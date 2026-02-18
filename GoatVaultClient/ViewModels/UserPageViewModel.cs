using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Account;
using GoatVaultApplication.Auth;
using GoatVaultApplication.VaultUseCases;
using GoatVaultClient.Controls.Popups;
using GoatVaultClient.Services;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;
using Mopups.Services;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.ViewModels;

public partial class UserPageViewModel : BaseViewModel
{
    private readonly LoadUserProfileUseCase _loadUserProfile;
    private readonly CalculateVaultScoreUseCase _calculateVaultScore;
    private readonly ChangeEmailUseCase _changeEmail;
    private readonly ChangePasswordUseCase _changePassword;
    private readonly EnableMfaUseCase _enableMfa;
    private readonly DisableMfaUseCase _disableMfa;
    private readonly LogoutUseCase _logout;
    private readonly GoatTipsService _goatTips;
    private readonly ISessionContext _session;

    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private bool mfaEnabled;
    [ObservableProperty] private string? mfaSecret;
    [ObservableProperty] private string? mfaQrCodeUrl;

    [ObservableProperty] private double vaultScore;
    [ObservableProperty] private double masterPasswordStrength;
    [ObservableProperty] private double averagePasswordsStrength;
    [ObservableProperty] private double reuseRate;
    [ObservableProperty] private int breachesCount;
    [ObservableProperty] private double mfaPercent;
    [ObservableProperty] private string? vaultTierText;

    [ObservableProperty] private bool goatEnabled;

    public UserPageViewModel(
        LoadUserProfileUseCase loadUserProfile,
        CalculateVaultScoreUseCase calculateVaultScore,
        ChangeEmailUseCase changeEmail,
        ChangePasswordUseCase changePassword,
        EnableMfaUseCase enableMfa,
        DisableMfaUseCase disableMfa,
        LogoutUseCase logout,
        GoatTipsService goatTips,
        ISessionContext session)
    {
        _loadUserProfile = loadUserProfile;
        _calculateVaultScore = calculateVaultScore;
        _changeEmail = changeEmail;
        _changePassword = changePassword;
        _enableMfa = enableMfa;
        _disableMfa = disableMfa;
        _logout = logout;
        _goatTips = goatTips;
        _session = session;

        GoatEnabled = _goatTips.IsGoatEnabled;

        _session.VaultChanged += OnVaultChanged;

        // Initial load
        Task.Run(InitializeAsync);
    }

    private void OnVaultChanged(object? sender, EventArgs e) => Task.Run(RefreshVaultScoreAsync);

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var user = await _loadUserProfile.ExecuteAsync();
            Email = user.Email.Value;
            MfaEnabled = user.MfaEnabled;

            await RefreshVaultScoreAsync();
        }
        catch (Exception ex)
        {
            // Handle error (log or show alert)
            Console.WriteLine($"Error loading user profile: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshVaultScoreAsync()
    {
        try
        {
            var details = await _calculateVaultScore.ExecuteAsync();

            // Updates to observable properties bound to UI must be on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MasterPasswordStrength = details.MasterPasswordPercent / 100.0 * 400;
                AveragePasswordsStrength = details.AveragePasswordsPercent / 100.0 * 200;
                ReuseRate = details.ReuseRatePercent / 100.0 * 200;
                MfaPercent = details.MfaEnabled ? 200 : 0;

                VaultScore = details.VaultScore;
                VaultTierText = $"{GetVaultTier(VaultScore)} ({VaultScore:0}/1000)";
                BreachesCount = details.BreachesCount;

                OnPropertyChanged(nameof(MasterPasswordCategory));
                OnPropertyChanged(nameof(AveragePasswordsCategory));
                OnPropertyChanged(nameof(OriginalityCategory));
                OnPropertyChanged(nameof(BreachesCategory));
                OnPropertyChanged(nameof(MfaCategory));
            });
        }
        catch
        {
            // Ignore if session ended or error
        }
    }

    private static string GetVaultTier(double score) => score switch
    {
        >= 900 => "The Summit Sovereign (900+)",
        >= 750 => "The Ridge Walker (750-899)",
        >= 500 => "The Cliffside Scrambler (500-749)",
        >= 300 => "The Treeline Grazer (300-499)",
        _ => "The Dead Meat (< 300)"
    };

    public string MasterPasswordCategory => MasterPasswordStrength switch
    {
        300 => "Very Strong",
        200 => "Strong",
        100 => "Weak",
        _ => "Poor"
    };

    public string AveragePasswordsCategory => AveragePasswordsStrength switch
    {
        200 => "Very Strong",
        150 => "Strong",
        100 => "Weak",
        _ => "Poor"
    };

    public string OriginalityCategory => ReuseRate switch
    {
        200 => "Very Strong",
        150 => "Strong",
        100 => "Weak",
        _ => "Poor"
    };

    public string BreachesCategory => BreachesCount switch
    {
        0 => "Very Strong",
        1 => "Strong",
        2 => "Weak",
        _ => "Poor"
    };

    public string MfaCategory => MfaEnabled ? "Very Strong" : "Poor";

    [RelayCommand]
    private void ToggleGoat()
    {
        GoatEnabled = !GoatEnabled;
        _goatTips.SetEnabled(GoatEnabled);
    }

    [RelayCommand]
    private async Task EditEmailAsync()
    {
        if (IsBusy) return;

        var password = await PromptPasswordAsync("Confirm Password");
        if (password == null) return;

        var newEmailStr = await PromptInputAsync("Enter new email");
        if (string.IsNullOrWhiteSpace(newEmailStr)) return;

        if (newEmailStr == Email) return;

        try
        {
            IsBusy = true;
            await _changeEmail.ExecuteAsync(password, new Email(newEmailStr));
            Email = newEmailStr;
            await ShowSuccessAsync("Email updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditMasterPasswordAsync()
    {
        if (IsBusy) return;

        var currentPassword = await PromptPasswordAsync("Confirm Current Password");
        if (currentPassword == null) return;

        var newPassword = await PromptPasswordAsync("Enter New Master Password");
        if (string.IsNullOrWhiteSpace(newPassword)) return;

        try
        {
            IsBusy = true;
            await _changePassword.ExecuteAsync(currentPassword, newPassword);
            await ShowSuccessAsync("Master password updated successfully.");
            await RefreshVaultScoreAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EnableMfaAsync()
    {
        if (IsBusy) return;

        var password = await PromptPasswordAsync("Confirm Password");
        if (password == null) return;

        try
        {
            IsBusy = true;

            // Generate secret locally
            var secret = TotpService.GenerateSecret();

            // Show QR/Secret to user
            await ShowMfaSetupDialogAsync(secret);

            // Ask for verification code
            var code = await PromptInputAsync("Enter 6-digit code from authenticator app");
            if (string.IsNullOrWhiteSpace(code)) return;

            // Verify locally first (sanity check)
            if (!TotpService.VerifyCode(secret, code))
            {
                await ShowErrorAsync("Invalid code. Please try again.");
                return;
            }

            // Enable on backend
            await _enableMfa.ExecuteAsync(password, secret);
            MfaEnabled = true;
            await ShowSuccessAsync("MFA enabled successfully.");
            await RefreshVaultScoreAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisableMfaAsync()
    {
        if (IsBusy) return;

        var confirm = await ShowConfirmationAsync("Disable MFA", "Are you sure you want to disable two-factor authentication?");
        if (!confirm) return;

        var password = await PromptPasswordAsync("Confirm Password");
        if (password == null) return;

        try
        {
            IsBusy = true;
            await _disableMfa.ExecuteAsync(password);
            MfaEnabled = false;
            await ShowSuccessAsync("MFA disabled successfully.");
            await RefreshVaultScoreAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _session.VaultChanged -= OnVaultChanged;
            await _logout.ExecuteAsync();
            await Shell.Current.GoToAsync("//intro");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #region UI Helpers

    private static async Task<string?> PromptPasswordAsync(string title)
    {
        var popup = new AuthorizePopup(title, isPassword: true);
        await MopupService.Instance.PushAsync(popup);
        return await popup.WaitForScan();
    }

    private static async Task<string?> PromptInputAsync(string title)
    {
        var popup = new AuthorizePopup(title, isPassword: false);
        await MopupService.Instance.PushAsync(popup);
        return await popup.WaitForScan();
    }

    private static async Task ShowErrorAsync(string message)
    {
        await MopupService.Instance.PushAsync(new PromptPopup("Error", message, "OK"));
    }

    private static async Task ShowSuccessAsync(string message)
    {
        await MopupService.Instance.PushAsync(new PromptPopup("Success", message, "OK"));
    }

    private static async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var popup = new PromptPopup(title, message, "Yes", "No");
        await MopupService.Instance.PushAsync(popup);
        return await popup.WaitForScan();
    }

    private static async Task ShowMfaSetupDialogAsync(string secret)
    {
         var message = $"""
                        Scan this QR code with your authenticator app:
                        
                        Or enter this secret manually:
                        
                        {secret}
                        
                        Recommended apps:
                        - Google Authenticator
                        - Microsoft Authenticator
                        - Authy
                        """;
         // Note: Actual QR code image generation isn't implemented here, just text.
         // A real implementation would generate a QR image.

        var setupPopup = new PromptPopup(
            title: "Setup MFA",
            body: message,
            aText: "OK"
        );
        await MopupService.Instance.PushAsync(setupPopup);
        await setupPopup.WaitForScan();

        await Clipboard.Default.SetTextAsync(secret);
        await ShowSuccessAsync("Secret copied to clipboard.");
    }

    #endregion
}