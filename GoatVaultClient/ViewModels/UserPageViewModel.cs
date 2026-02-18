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
using System.Collections.ObjectModel;
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
    [ObservableProperty] private bool goatEnabled;

    [ObservableProperty] private double vaultScore;
    [ObservableProperty] private string? vaultTierText;

    [ObservableProperty] private bool showVaultDetails;
    [ObservableProperty] private ObservableCollection<VaultMetricItem> vaultMetrics = [];

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

    private void OnVaultChanged(object? sender, EventArgs e)
    {
        Task.Run(RefreshVaultScoreAsync);
    }

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
                VaultScore = details.VaultScore;
                VaultTierText = GetVaultTier(VaultScore);

                var metricItem = new VaultMetricItem
                {
                    MasterPasswordStrength = $"{details.MasterPasswordPercent}%",
                    AveragePasswordsStrength = $"{details.AveragePasswordsPercent}%",
                    ReuseRateText = $"{details.ReuseRatePercent}%",
                    BreachesText = $"{details.BreachesCount}",
                    MfaStatusText = details.MfaEnabled ? "Enabled" : "Disabled"
                };

                VaultMetrics.Clear();
                VaultMetrics.Add(metricItem);
            });
        }
        catch (Exception)
        {
            // Ignore if session ended or error
        }
    }

    [RelayCommand]
    private async Task ShowVaultDetailsPopupAsync()
    {
        if (VaultMetrics.Count == 0) return;
        var m = VaultMetrics[0];

        var message =
            $"Tier: {VaultTierText}\n" +
            $"\nMaster password: {m.MasterPasswordStrength}" +
            $"\nAverage passwords: {m.AveragePasswordsStrength}" +
            $"\nOriginality: {m.ReuseRateText}" +
            $"\nBreached passwords: {m.BreachesText}" +
            $"\nMFA: {m.MfaStatusText}";

        await MopupService.Instance.PushAsync(new PromptPopup(
            title: "Vault Score Details",
            body: message,
            aText: "OK"
        ));
    }

    private static string GetVaultTier(double score)
    {
        return score switch
        {
            >= 900 => "The Summit Sovereign (900+)",
            >= 750 => "The Ridge Walker (750-899)",
            >= 500 => "The Cliffside Scrambler (500-749)",
            >= 300 => "The Treeline Grazer (300-499)",
            _ => "The Dead Meat (< 300)"
        };
    }

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
            await ShowMfaSetupDialogAsync(secret, Email);

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

public class VaultMetricItem
{
    public string MasterPasswordStrength { get; set; } = string.Empty;
    public string AveragePasswordsStrength { get; set; } = string.Empty;
    public string ReuseRateText { get; set; } = string.Empty;
    public string BreachesText { get; set; } = string.Empty;
    public string MfaStatusText { get; set; } = string.Empty;
}