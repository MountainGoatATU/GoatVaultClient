using CommunityToolkit.Mvvm.ComponentModel;

namespace GoatVaultCore.Models;

public partial class VaultEntry : ObservableObject
{
    [ObservableProperty] private string _site = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _category = string.Empty;

    // MFA
    [ObservableProperty] private string _mfaSecret = string.Empty;
    [ObservableProperty] private bool _hasMfa;
    [ObservableProperty] private string? _currentTotpCode;
    [ObservableProperty] private int _totpTimeRemaining;

    // Vault score
    [ObservableProperty] private int _breachCount;
}