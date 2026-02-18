using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using PasswordGenerator;
using GoatVaultCore.Services;

namespace GoatVaultCore.Models;

public partial class VaultEntryForm : VaultEntry
{

    public VaultEntryForm(List<CategoryItem>? categories)
    {
        AvailableCategories = categories ?? [];

        IncludeLowercase = true;
        IncludeUppercase = true;
        IncludeNumbers = true;
        IncludeSpecial = true;

        PasswordLength = 16;

        PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MfaSecret):
                    ValidateMfaSecret();
                    break;
                case nameof(Password):
                    EvaluatePasswordStrength();
                    break;
            }
        };
    }

    [ObservableProperty] [property: Required] private List<CategoryItem> _availableCategories;
    [ObservableProperty] private string? _crackTime;
    [ObservableProperty] private double _crackProgress;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _includeLowercase = true;
    [ObservableProperty] private bool _includeUppercase = true;
    [ObservableProperty] private bool _includeNumbers = true;
    [ObservableProperty] private bool _includeSpecial = true;
    [ObservableProperty] private int _passwordLength = 16;

    // MFA validation property
    [ObservableProperty] private bool _isMfaSecretValid;
    [ObservableProperty] private string? _mfaValidationMessage;

    private void EvaluatePasswordStrength()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            CrackTime = null;
            CrackProgress = 0;
            return;
        }

        var result = PasswordStrengthService.Evaluate(Password);

        CrackTime = $"Crack time: {result.CrackTimeText}";

        CrackProgress = result.Score / 4.0;
    }

    private void ValidateMfaSecret()
    {
        // Validate MFA secret
        if (string.IsNullOrWhiteSpace(MfaSecret))
        {
            HasMfa = false;
            IsMfaSecretValid = false;
            MfaValidationMessage = null;
            return;
        }

        IsMfaSecretValid = TotpService.IsValidSecret(MfaSecret);

        if (IsMfaSecretValid)
        {
            HasMfa = true;
            MfaValidationMessage = "✓ Valid MFA secret";
        }
        else
        {
            HasMfa = false;
            MfaValidationMessage = "✗ Invalid MFA secret (must be Base32)";
        }
    }

    [RelayCommand]
    private void GeneratePassword()
    {
        if (!IncludeLowercase && !IncludeUppercase && !IncludeNumbers && !IncludeSpecial)
        {
            IncludeLowercase = true;
        }

        var generator = new Password();

        if (IncludeLowercase) generator.IncludeLowercase();
        if (IncludeUppercase) generator.IncludeUppercase();
        if (IncludeNumbers) generator.IncludeNumeric();
        if (IncludeSpecial) generator.IncludeSpecial();

        Password = generator.LengthRequired(PasswordLength).Next();
    }
}