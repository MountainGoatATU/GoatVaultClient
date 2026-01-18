using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Services;
using PasswordGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Icons.MaterialSymbols;
using UraniumUI.Options;

namespace GoatVaultClient_v3.Models
{
    public partial class VaultEntryForm : VaultEntry
    {
        private readonly PasswordStrengthService _passwordStrength;

        public VaultEntryForm(PasswordStrengthService passwordStrength, List<CategoryItem> categories)
        {
            _passwordStrength = passwordStrength;
            AvailableCategories = categories ?? new List<CategoryItem>();
        }

        [ObservableProperty]
        [property: Required]
        private List<CategoryItem> availableCategories;

        [ObservableProperty]
        private string? crackTime;

        [ObservableProperty]
        private double crackProgress;

        [ObservableProperty]
        private string password;

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
            var passwordGenerator = new Password();
            Password = passwordGenerator.IncludeLowercase()
                                        .IncludeUppercase()
                                        .IncludeNumeric()
                                        .IncludeSpecial()
                                        .LengthRequired(22)
                                        .Next();
        }
    }
}
