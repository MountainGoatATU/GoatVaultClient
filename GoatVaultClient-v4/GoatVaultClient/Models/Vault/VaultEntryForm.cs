using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using PasswordGenerator;
using GoatVaultClient.Services.Secrets;

namespace GoatVaultClient.Models.Vault;

public partial class VaultEntryForm : VaultEntry
{
    private readonly PasswordStrengthService _passwordStrength;

    public VaultEntryForm(PasswordStrengthService passwordStrength, List<CategoryItem>? categories)
    {
        _passwordStrength = passwordStrength;
        AvailableCategories = categories ?? [];

        IncludeLowercase = true;
        IncludeUppercase = true;
        IncludeNumbers = true;
        IncludeSpecial = true;

        PasswordLength = 16;
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

    partial void OnPasswordChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CrackTime = null;
            CrackProgress = 0;
            return;
        }

        var result = _passwordStrength.Evaluate(value);

        CrackTime = $"Crack time: {result.CrackTimeText}";

        CrackProgress = result.Score / 4.0;
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