using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Vault;
using System.Collections.ObjectModel;
using GoatVaultClient.Services;
using GoatVaultCore.Services.Secrets;

namespace GoatVaultClient.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        #region Properties
        // Observables
        [ObservableProperty] private ObservableCollection<CategoryItem> categories = new();
        [ObservableProperty] private ObservableCollection<VaultEntry> passwords = new();
        [ObservableProperty] private CategoryItem? selectedCategory;
        [ObservableProperty] private VaultEntry? selectedEntry;
        [ObservableProperty] private string? searchText;
        [ObservableProperty] private bool _isPasswordVisible;
        [ObservableProperty] private double vaultScore;
        [ObservableProperty] private string goatComment = string.Empty;
        [ObservableProperty] private bool isGoatCommentVisible;
        [ObservableProperty] private bool _isSyncing;

        // UI Properties for the Sync Status Component
        private bool _categoriesSortAsc = true;
        private bool _passwordsSortAsc = true;
        private List<VaultEntry> _allVaultEntries = [];
        private List<CategoryItem> _allVaultCategories = [];
        private bool CanSync() => !IsBusy;

        // Dependency Injection
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly ISyncingService _syncingService;
        private readonly TotpManagerService _totpManagerService;
        private readonly CategoryManagerService _categoryManagerService;
        private readonly VaultEntryManagerService _vaultEntryManagerService;
        private readonly PwnedPasswordService _pwnedPasswordService;
        public GoatTipsService GoatTipsService { get; }

        #endregion
        public MainPageViewModel(
            VaultSessionService vaultSessionService,
            FakeDataSource fakeDataSource,
            ISyncingService syncingService,
            GoatTipsService goatTipsService,
            TotpManagerService totpManagerService,
            CategoryManagerService categoryManagerService,
            VaultEntryManagerService vaultEntryManagerService,
            PwnedPasswordService pwnedPasswordService)
        {
            // Dependency Injection
            _vaultSessionService = vaultSessionService;
            _fakeDataSource = fakeDataSource;
            _syncingService = syncingService;
            GoatTipsService = goatTipsService;
            _totpManagerService = totpManagerService;
            _categoryManagerService = categoryManagerService;
            _vaultEntryManagerService = vaultEntryManagerService;
            _pwnedPasswordService = pwnedPasswordService;

            _syncingService.SyncCompleted += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectedEntry = null; // Clear selection to prevent editing a dead reference
                    ReloadVaultData();
                });
            };

            LoadVaultData();
            StartRandomGoatComments();
        }

        partial void OnSelectedEntryChanged(VaultEntry? value) => _totpManagerService.TrackEntry(value);

        [RelayCommand]
        private async Task CopyTotpCode() => await _totpManagerService.CopyTotpCodeAsync(SelectedEntry);

        public void StartRandomGoatComments()
        {
            GoatTipsService.StartTips();

            GoatTipsService.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(GoatTipsService.CurrentTip):
                        GoatComment = GoatTipsService.CurrentTip;
                        break;

                    case nameof(GoatTipsService.IsTipVisible):
                    case nameof(GoatTipsService.IsGoatEnabled):
                        var show = GoatTipsService is { IsGoatEnabled: true, IsTipVisible: true };
                        IsGoatCommentVisible = show;
                        if (!show)
                            GoatComment = string.Empty;
                        break;
                }
            };
        }
        #region Synchronous methods
        public void LoadVaultData()
        {
            ReloadVaultData();
        }
        private void ReloadVaultData()
        {
            // If vault is not decrypted, return
            if (_vaultSessionService.DecryptedVault == null)
                return;

            // Clear Passwords and Categories
            Categories.Clear();
            Passwords.Clear();

            // Reload from decrypted vault into local cache
            _allVaultEntries = _vaultSessionService.DecryptedVault.Entries.ToList();
            _allVaultCategories = _vaultSessionService.DecryptedVault.Categories.ToList();

            // Set observable collections
            UpdateCollection(Categories, _allVaultCategories);
            UpdateCollection(Passwords, _allVaultEntries);

            // Presort to match UI
            PresortEntries(true);
            PresortCategories(true);
        }

        private void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            collection.Clear();
            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    collection.Add(item);
                }
            }
        }

        private void PresortCategories(bool matchUi = false)
        {
            if (!matchUi)
            {
                _categoriesSortAsc = !_categoriesSortAsc;
            }

            var sortedCategories = VaultFilterService.FilterAndSortCategories(
                _allVaultCategories,
                SearchText,
                _categoriesSortAsc);

            UpdateCollection(Categories, sortedCategories);
        }

        private void PresortEntries(bool matchUi = false)
        {
            if (!matchUi)
            {
                _passwordsSortAsc = !_passwordsSortAsc;
            }

            var sortedEntries = VaultFilterService.FilterAndSortEntries(
                _allVaultEntries,
                SearchText,
                SelectedCategory?.Name == "All" ? null : SelectedCategory?.Name,
                _passwordsSortAsc);

            UpdateCollection(Passwords, sortedEntries);
        }

        partial void OnSelectedCategoryChanged(CategoryItem? value)
        {
            SearchText = "";

            if (value?.Name == "All")
            {
                ReloadVaultData();
            }
            else
            {
                var filteredEntries = VaultFilterService.FilterAndSortEntries(
                    _allVaultEntries,
                    SearchText,
                    value?.Name,
                    _passwordsSortAsc);

                UpdateCollection(Passwords, filteredEntries);
            }
        }

        partial void OnSearchTextChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // Filter Categories
                var filteredCategories = VaultFilterService.FilterAndSortCategories(
                    _allVaultCategories, // Filter from ALL
                    SearchText,
                    _categoriesSortAsc);
                UpdateCollection(Categories, filteredCategories);

                // Filter Passwords
               var filteredEntries = VaultFilterService.FilterAndSortEntries(
                    _allVaultEntries, // Filter from ALL
                    SearchText,
                    SelectedCategory?.Name == "All" ? null : SelectedCategory?.Name, // Keep category filter
                    _passwordsSortAsc);
                UpdateCollection(Passwords, filteredEntries);
            }
            else
            {
                ReloadVaultData();
            }
        }
        #endregion

        #region Commands
        /*
         * Sync commands
         */
        // Manual Sync Command
        [RelayCommand]
        private async Task Save()
        {
            // Indicate syncing
            IsSyncing = true;

            // Save to local and server
            //await _syncingService.Save();
            await _syncingService.Sync();

            // Reset syncing indicator
            IsSyncing = false;
        }

        /*
         * Categories Commands
         */

        [RelayCommand]
        private Task SortCategories()
        {
            PresortCategories();
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task CreateCategory()
        {
            var changed = await _categoryManagerService.CreateCategoryAsync(Categories);
            if (changed)
            {
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task EditCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;
            var changed = await _categoryManagerService.EditCategoryAsync(target);
            if (changed)
            {
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task DeleteCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;
            var changed = await _categoryManagerService.DeleteCategoryAsync(target);
            if (changed)
            {
                // Set selected category
                SelectedCategory = null;
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private Task SortEntries()
        {
            PresortEntries();
            return Task.CompletedTask;
        }

        // TODO: Unused method parameter
        [RelayCommand]
        private async Task CopyEntry()
        {
            await VaultEntryManagerService.CopyEntryPasswordAsync(SelectedEntry);
        }

        [RelayCommand]
        private async Task CreateEntry()
        {
            var changed = await _vaultEntryManagerService.CreateEntryAsync(Categories);
            if (changed)
            {
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task EditEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;
            var changed = await _vaultEntryManagerService.EditEntryAsync(target, Categories);
            if (changed)
            {
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task DeleteEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;
            var changed = await _vaultEntryManagerService.DeleteEntryAsync(target);
            if (changed)
            {
                // Update UI
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }
}