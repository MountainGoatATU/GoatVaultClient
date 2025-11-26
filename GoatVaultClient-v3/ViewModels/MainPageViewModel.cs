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
        private readonly VaultSessionService _vaultSessionService;

        public MainPageViewModel(VaultService vaultService, HttpService httpService, UserService userService, VaultSessionService vaultSessionService)
        {
            //Dependency Injection
            _vaultSessionService = vaultSessionService;

            Categories = new ObservableCollection<string>();
            Passwords = new ObservableCollection<VaultEntry>();
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
                _vaultSessionService.DecryptedVault.Categories.Add(result.Trim());
                LoadVaultData();
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

        // 1. Create a method to load the data
        public void LoadVaultData()
        {
            // If the session is empty (user logged out or not ready), do nothing
            if (_vaultSessionService.DecryptedVault == null)
                return;

            // 2. Reload Categories
            // We clear and add instead of 'new ObservableCollection' to keep UI bindings stable, 
            // though replacing the collection works too if PropertyChanged is fired.
            if (_vaultSessionService.DecryptedVault.Categories != null)
            {
                Categories = new ObservableCollection<string>(_vaultSessionService.DecryptedVault.Categories);
            }

            // 3. Reload Passwords
            if (_vaultSessionService.DecryptedVault.Entries != null)
            {
                Passwords = new ObservableCollection<VaultEntry>(_vaultSessionService.DecryptedVault.Entries);
            }
        }

        // 4. Trigger this method when the page appears
        [RelayCommand]
        private void Appearing()
        {
            LoadVaultData();
        }
    }
}
