using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        [ObservableProperty]
        [property: Required]
        private List<CategoryItem> availableCategories;

        [RelayCommand]
        private void GeneratePassword()
        {
            var passwordGenerator = new Password();
            Password = passwordGenerator.IncludeLowercase()
                                        .IncludeUppercase()
                                        .IncludeNumeric()
                                        .IncludeSpecial()
                                        .LengthRequired(16)
                                        .Next();
        }
    }
}
