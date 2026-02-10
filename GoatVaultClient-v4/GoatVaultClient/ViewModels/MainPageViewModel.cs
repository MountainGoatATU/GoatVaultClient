using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Vault;
using System.Collections.ObjectModel;
using GoatVaultCore.Services.Secrets;
using GoatVaultClient.Services;

namespace GoatVaultClient.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        #region Properties
        /*
         * Observables
         */
        [ObservableProperty] private ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty] private ObservableCollection<VaultEntry> passwords = [];
        [ObservableProperty] private CategoryItem? selectedCategory = null;
        [ObservableProperty] private VaultEntry? selectedEntry = null;
        [ObservableProperty] private string? searchText = null;
        private bool _categoriesSortAsc = true;
        private bool _passwordsSortAsc = true;
        private List<VaultEntry> _allVaultEntries = [];
        private List<CategoryItem> _allVaultCategories = [];
        private bool CanSync() => !IsBusy;
        [ObservableProperty] private bool _isPasswordVisible = false;
        [ObservableProperty] private double vaultScore;
        [ObservableProperty] private string goatComment = "";
        [ObservableProperty] private bool isGoatCommentVisible = false;

        // UI Properties for the Sync Status Component
        [ObservableProperty] private bool _isSyncing = false;

        // Dependency Injection
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly ISyncingService _syncingService;
        private readonly GoatTipsService _goatTipsService;
        private readonly TotpManagerService _totpManagerService;
        private readonly CategoryManagerService _categoryManagerService;
        private readonly VaultEntryManagerService _vaultEntryManagerService;
        public GoatTipsService GoatTipsService => _goatTipsService;

        #endregion
        public MainPageViewModel(
            VaultSessionService vaultSessionService,
            FakeDataSource fakeDataSource,
            ISyncingService syncingService,
            GoatTipsService goatTipsService,
            TotpManagerService totpManagerService,
            CategoryManagerService categoryManagerService,
            VaultEntryManagerService vaultEntryManagerService)
        {
            // Dependency Injection
            _vaultSessionService = vaultSessionService;
            _fakeDataSource = fakeDataSource;
            _syncingService = syncingService;
            _goatTipsService = goatTipsService;
            _totpManagerService = totpManagerService;
            _categoryManagerService = categoryManagerService;
            _vaultEntryManagerService = vaultEntryManagerService;

            LoadVaultData();

            StartRandomGoatComments();
        }

        partial void OnSelectedEntryChanged(VaultEntry? value)
        {
            // Update TOTP immediately when entry is selected
            _totpManagerService.TrackEntry(value);
        }

        [RelayCommand]
        private async Task CopyTotpCode()
        {
            await _totpManagerService.CopyTotpCodeAsync(SelectedEntry);
        }

        public void StartRandomGoatComments()
        {
            _goatTipsService.StartTips();

            _goatTipsService.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(GoatTipsService.CurrentTip):
                        GoatComment = _goatTipsService.CurrentTip;
                        break;

                    case nameof(GoatTipsService.IsTipVisible):
                    case nameof(GoatTipsService.IsGoatEnabled):
                        var show = _goatTipsService.IsGoatEnabled && _goatTipsService.IsTipVisible;
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
            if (!Categories.Any() && !Passwords.Any())
            {
                // TODO: TEST DATA IF USER NOT LOGGED IN
                // Seeding Categories
                Categories.Clear();
                Categories.Add(new CategoryItem { Name = "All" });
                Categories = _fakeDataSource.GetFolderItems().ToObservableCollection();

                // Adding Default Category
                PresortCategories(true);

                // Seeding Passwords
                Passwords.Clear();
                Passwords = _fakeDataSource.GetVaultEntryItems(10).ToObservableCollection();
                PresortEntries(true);
            }
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
            Passwords = _allVaultEntries.ToObservableCollection();
            Categories = _allVaultCategories.ToObservableCollection();

            // Presort to match UI
            PresortEntries(true);
            PresortCategories(true);

            // Recalculate Vault Score
            CalculateVaultScore();
        }

        private void PresortCategories(bool matchUi = false)
        {
            if (!matchUi)
            {
                _categoriesSortAsc = !_categoriesSortAsc;
            }

            Categories = VaultFilterService.FilterAndSortCategories(
                Categories,
                SearchText,
                _categoriesSortAsc);
        }

        private void PresortEntries(bool matchUi = false)
        {
            if (!matchUi)
            {
                _passwordsSortAsc = !_passwordsSortAsc;
            }

            Passwords = VaultFilterService.FilterAndSortEntries(
                Passwords,
                null,
                null,
                _passwordsSortAsc);
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
                Passwords = VaultFilterService.FilterAndSortEntries(
                    _allVaultEntries,
                    null, // SearchText is cleared above
                    value?.Name,
                    _passwordsSortAsc);
            }
        }

        partial void OnSearchTextChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // Filter Categories
                Categories = VaultFilterService.FilterAndSortCategories(
                    _allVaultCategories, // Filter from ALL
                    SearchText,
                    _categoriesSortAsc);

                // Filter Passwords
                Passwords = VaultFilterService.FilterAndSortEntries(
                    _allVaultEntries, // Filter from ALL
                    SearchText,
                    SelectedCategory?.Name == "All" ? null : SelectedCategory?.Name,
                    _passwordsSortAsc);
            }
            else
            {
                ReloadVaultData();
            }
        }

        private void CalculateVaultScore()
        {
            var scoreDetails = VaultScoreCalculatorService.CalculateScore(
                Passwords,
                null,      // masterPassword
                false      // mfaEnabled
            );

            VaultScore = scoreDetails.VaultScore;
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
        private async Task SortCategories()
        {
            PresortCategories();
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
        private async Task SortEntries()
        {
            PresortEntries();
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
                // Sort to match UI
                PresortEntries(true);

                // Update UI
                Passwords = _allVaultEntries.ToObservableCollection();
                CalculateVaultScore();
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
                CalculateVaultScore();
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
                CalculateVaultScore();
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