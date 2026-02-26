using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GoatVaultCore.Models;

public partial class VaultEntry : ObservableObject
{
    [ObservableProperty] [property: Required] private string _site = string.Empty;
    [ObservableProperty] [property: Required] private string _userName = string.Empty;
    [ObservableProperty] [property: Required] private string _password = string.Empty;
    [ObservableProperty] [property: Required] private string _description = string.Empty;
    [ObservableProperty] [property: Required] private string _category = string.Empty;

    // MFA
    [ObservableProperty] [property: Required] private string _mfaSecret = string.Empty;
    [ObservableProperty] [property: Required] private bool _hasMfa;
    [ObservableProperty] [property: Required] private string? _currentTotpCode;
    [ObservableProperty] [property: Required] private int _totpTimeRemaining;

    // Vault score
    [ObservableProperty] [property: Required] private int _breachCount;
}