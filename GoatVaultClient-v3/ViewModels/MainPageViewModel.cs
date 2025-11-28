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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        #region Properties
        /*
         * Observables
         */
        private List<VaultEntry> _allVaultEntries = new List<VaultEntry>();
        [ObservableProperty]
        private ObservableCollection<string> categories;
        [ObservableProperty]
        private ObservableCollection<VaultEntry> passwords;
        [ObservableProperty]
        private string selectedCategory;

        /*
         * Visibility Toggles for Forms and Lists
         */
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEntriesListVisible))] // Updates the list visibility when form visibility changes
        private bool isEntryFormVisible;
        public bool IsEntriesListVisible => !IsEntryFormVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCategoryListVisible))]
        private bool isCategoryFormVisible;
        public bool IsCategoryListVisible => !IsCategoryFormVisible;

        /*
         * Input Fields for new Entry and Category
         */
        [ObservableProperty]
        private string newCategoryName;

        [ObservableProperty]
        private string newEntryDescription;

        [ObservableProperty]
        private string newEntryUsername;

        [ObservableProperty]
        private string newEntryPassword;

        [ObservableProperty]
        private string newEntrySelectedCategory;

        private bool _isSortedAscending = true;

        private string _editingCategory;

        private VaultEntry _editingEntry;

        //Dependency Injection
        private readonly VaultSessionService _vaultSessionService;
        #endregion
        public MainPageViewModel(VaultService vaultService, HttpService httpService, UserService userService, VaultSessionService vaultSessionService)
        {
            //Dependency Injection
            _vaultSessionService = vaultSessionService;

            Categories = new ObservableCollection<string>();
            Passwords = new ObservableCollection<VaultEntry>();
        }
        #region Synchronous methods
        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyFilter();
        }

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
                Categories.Clear();
                Categories = new ObservableCollection<string>(_vaultSessionService.DecryptedVault.Categories);
            }

            // 3. Reload Passwords
            if (_vaultSessionService.DecryptedVault.Entries != null)
            {
                _allVaultEntries = _vaultSessionService.DecryptedVault.Entries;

                ApplyFilter();
            }
        }

        // 5. ADD: The Filter Logic
        private void ApplyFilter()
        {
            IEnumerable<VaultEntry> filtered;

            if (string.IsNullOrEmpty(SelectedCategory))
            {
                // If no category selected, show all
                filtered = _allVaultEntries;
            }
            else
            {
                // Assuming VaultEntry has a 'Category' property. 
                // If not, you will need to add it to your Model.
                filtered = _allVaultEntries.Where(x => x.Category == SelectedCategory);
            }

            Passwords = new ObservableCollection<VaultEntry>(filtered);
        }
        #endregion
        #region Commands
        /*
         * Categories Commands
         */
        [RelayCommand]
        private void SortCategories()
        {
            // Toggle sort order
            var sorted = _isSortedAscending
                ? Categories.OrderBy(f => f).ToList()
                : Categories.OrderByDescending(f => f).ToList();

            Categories = new ObservableCollection<string>(sorted);
            _isSortedAscending = !_isSortedAscending;
        }

        [RelayCommand]
        private async Task CreateCategory()
        {
            NewCategoryName = string.Empty;
            IsCategoryFormVisible = true; // Hides list, shows form
        }

        [RelayCommand]
        private void EditCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return;

            NewCategoryName = category;
            _editingCategory = category;
            IsCategoryFormVisible = true;
        }

        [RelayCommand]
        private void DeleteCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return;

            if (_vaultSessionService.DecryptedVault.Categories.Contains(category))
            {
                _vaultSessionService.DecryptedVault.Categories.Remove(category);

                // Remove all entries in that category
                var entriesToRemove = _vaultSessionService.DecryptedVault.Entries
                    .Where(e => e.Category == category)
                    .ToList();
                foreach (var entry in entriesToRemove)
                    _vaultSessionService.DecryptedVault.Entries.Remove(entry);

                LoadVaultData();
            }
        }

        [RelayCommand]
        private void CancelCategory()
        {
            IsCategoryFormVisible = false; // Shows list, hides form
        }

        // 5. ADD: Save Command
        [RelayCommand]
        private void SaveCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                return;

            if (!string.IsNullOrWhiteSpace(_editingCategory))
            {
                // Update existing category
                int index = _vaultSessionService.DecryptedVault.Categories.IndexOf(_editingCategory);
                if (index >= 0)
                    _vaultSessionService.DecryptedVault.Categories[index] = NewCategoryName.Trim();

                // Update category in existing entries
                foreach (var entry in _vaultSessionService.DecryptedVault.Entries
                             .Where(e => e.Category == _editingCategory))
                {
                    entry.Category = NewCategoryName.Trim();
                }

                _editingCategory = null;
            }
            else
            {
                // Add new category
                _vaultSessionService.DecryptedVault.Categories.Add(NewCategoryName.Trim());
            }

            // Refresh the UI list
            LoadVaultData();

            IsCategoryFormVisible = false;
        }
        /*
         * Entries Commands
         */
        [RelayCommand]
        private void SortEntries()
        {
            throw new NotImplementedException("CopyEntry command is not implemented yet.");
        }

        [RelayCommand]
        private async Task CopyEntry(string password)
        {
            if (string.IsNullOrEmpty(password))
                return;

            // Copy to clipboard
            await Clipboard.Default.SetTextAsync(password);

            // Show confirmation
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Copied", "Password copied to clipboard", "OK");
            }
        }

        [RelayCommand]
        private async Task CreateEntry()
        {
            NewEntryDescription = string.Empty;
            NewEntryUsername = string.Empty;
            NewEntryPassword = string.Empty;

            NewEntrySelectedCategory = SelectedCategory ?? Categories.FirstOrDefault();

            IsEntryFormVisible = true;
        }

        [RelayCommand]
        private void EditEntry(VaultEntry entry)
        {
            if (entry == null)
                return;

            NewEntryDescription = entry.Description;
            NewEntryUsername = entry.UserName;
            NewEntryPassword = entry.Password;
            NewEntrySelectedCategory = entry.Category;

            _editingEntry = entry;

            IsEntryFormVisible = true;
        }

        [RelayCommand]
        private void DeleteEntry(VaultEntry entry)
        {
            if (entry == null)
                return;

            _vaultSessionService.DecryptedVault.Entries.Remove(entry);
            LoadVaultData();
        }


        [RelayCommand]
        private void CancelEntry()
        {
            IsEntryFormVisible = false;
        }

        [RelayCommand]
        private void SaveEntry()
        {
            // Basic Validation
            if (string.IsNullOrWhiteSpace(NewEntryDescription) || string.IsNullOrWhiteSpace(NewEntryPassword))
                return;

            if (_editingEntry != null)
            {
                // Edit if exists
                _editingEntry.Description = NewEntryDescription;
                _editingEntry.UserName = NewEntryUsername;
                _editingEntry.Password = NewEntryPassword;
                _editingEntry.Category = NewEntrySelectedCategory;

                _editingEntry = null;
            }
            else
            {
                //Add new
                var newEntry = new VaultEntry
                {
                    Description = NewEntryDescription,
                    UserName = NewEntryUsername,
                    Password = NewEntryPassword,
                    Category = NewEntrySelectedCategory
                };
                // Add to the decrypted vault
                _vaultSessionService.DecryptedVault.Entries.Add(newEntry);
            }

            // Hide the entry form
            IsEntryFormVisible = false;
            // Reload the data to reflect the new entry
            LoadVaultData();
        }

        // 4. Trigger this method when the page appears
        [RelayCommand]
        private void Appearing()
        {
            LoadVaultData();
        }
        #endregion
    }
}
