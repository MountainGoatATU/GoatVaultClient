using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        // The list bound to the CollectionView
        [ObservableProperty]
        private ObservableCollection<string> categories;
        [ObservableProperty]
        private ObservableCollection<VaultEntry> passwords;

        // Displayed in the Header
        [ObservableProperty]
        private string currentContextName = "My Vault";
        // This attribute generates the "IsBusy" property automatically

        private bool _isSortedAscending = true;

        //Dependency Injection
        private readonly HttpService _httpService;
        private readonly UserService _userService;
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;
        //private readonly SecretService _secretService;

        public MainPageViewModel(VaultService vaultService, HttpService httpService, UserService userService, VaultSessionService vaultSessionService)
        {
            //Dependency Injection
            _httpService = httpService;
            _userService = userService;
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
            if (_vaultSessionService.DecryptedVault == null)
            {
                throw new InvalidOperationException("Decrypted vault is not available in the session.");
            } else
            {
                Categories = new ObservableCollection<string>(_vaultSessionService.DecryptedVault.Categories);
                Passwords = new ObservableCollection<VaultEntry>(_vaultSessionService.DecryptedVault.Entries);
            }

            //// Dummy Data
            //Categories = new ObservableCollection<FolderItem>
            //{
            //    new FolderItem { Name = "Documents" },
            //    new FolderItem { Name = "Photos" },
            //    new FolderItem { Name = "Work Projects" },
            //    new FolderItem { Name = "Recipes" }
            //};
            //Passwords = new ObservableCollection<PasswordItem>
            //{
            //    new PasswordItem { Description = "Google", UserName = "john.doe@gmail.com", Password = "password123" },
            //    new PasswordItem { Description = "Netflix", UserName = "chill_user", Password = "secure!Password" },
            //    new PasswordItem { Description = "GitHub", UserName = "dev_guru", Password = "gitcommitpush" },
            //    new PasswordItem { Description = "Amazon", UserName = "shopper123", Password = "primeMember!" }
            //};
        }

        [RelayCommand]
        private void SortList()
        {
            // Toggle sort order
            var sorted = _isSortedAscending
                ? Categories.OrderBy(f => f).ToList()
                : Categories.OrderByDescending(f => f).ToList();

            Categories = new ObservableCollection<string>(sorted);
            _isSortedAscending = !_isSortedAscending;
        }

        [RelayCommand]
        private async Task CreateFolder()
        {
            // Using native MAUI DisplayPromptAsync for the input dialog
            // You can replace this with Mopups or a custom Uranium Popup if preferred
            string result = await Shell.Current.DisplayPromptAsync("New Folder", "Enter folder name:");

            if (!string.IsNullOrWhiteSpace(result))
            {
                Categories.Add(result.Trim().ToUpper());
            }
        }

        [RelayCommand]
        private async Task CopyPassword()
        {

        }

        [RelayCommand]
        private async Task CreatePassword()
        {

        }
    }
}
