using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Models;
using System.Collections.ObjectModel;
using GoatVaultApplication.VaultUseCases;
using GoatVaultClient.Services;
using GoatVaultCore.Abstractions;

namespace GoatVaultClient.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel, IDisposable
    {
        private readonly LoadVaultUseCase _loadVault;
        private readonly ISessionContext _sessionContext;

        #region Properties

        // Observables
        [ObservableProperty] private ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty] private ObservableCollection<VaultEntry> passwords = [];
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

        #endregion

        #region Dependency Injection

        private readonly ISyncingService _syncingService;
        private readonly TotpManagerService _totpManagerService;
        private readonly CategoryManagerService _categoryManagerService;
        private readonly VaultEntryManagerService _vaultEntryManagerService;
        // private readonly PwnedPasswordService _pwnedPasswordService;
        public GoatTipsService GoatTipsService { get; }

        #endregion

        public MainPageViewModel(
            LoadVaultUseCase loadVault,
            ISessionContext sessionContext,
            ISyncingService syncingService,
            GoatTipsService goatTipsService,
            TotpManagerService totpManagerService,
            CategoryManagerService categoryManagerService,
            VaultEntryManagerService vaultEntryManagerService/*,
            PwnedPasswordService pwnedPasswordService*/)
        {
            _loadVault = loadVault;
            _sessionContext = sessionContext;
            _syncingService = syncingService;
            GoatTipsService = goatTipsService;
            _totpManagerService = totpManagerService;
            _categoryManagerService = categoryManagerService;
            _vaultEntryManagerService = vaultEntryManagerService;
            // _pwnedPasswordService = pwnedPasswordService;

            // TODO: Check if logic is the same here
            _sessionContext.VaultChanged += OnVaultChanged;
            _syncingService.AuthenticationRequired += OnAuthenticationRequired;

            StartRandomGoatComments();
            Task.Run(async () => await InitializeAsync());
        }

        private void OnVaultChanged(object? sender, EventArgs e)
        {
             MainThread.BeginInvokeOnMainThread(() =>
             {
                 if (_sessionContext.Vault == null)
                     return;

                 _allVaultEntries = [.. _sessionContext.Vault.Entries];
                 _allVaultCategories = [.. _sessionContext.Vault.Categories];
                 SelectedEntry = null; // Clear selection to prevent editing a dead reference
                 ReloadVaultData();
             });
        }

        private async void OnAuthenticationRequired(object? sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current.MainPage.DisplayAlertAsync("Session Expired",
                    "Your password has changed on another device. Please log in again.", "OK");

                // Navigate to log in (using absolute route to clear stack)
                await Shell.Current.GoToAsync("//login");
            });
        }

        public void Dispose()
        {
            _sessionContext.VaultChanged -= OnVaultChanged;
            _syncingService.AuthenticationRequired -= OnAuthenticationRequired;
            GoatTipsService.PropertyChanged -= OnGoatTipsPropertyChanged;
        }

        private async Task InitializeAsync() => await LoadVaultAsync();

        [RelayCommand]
        private async Task LoadVaultAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                var vault = await _loadVault.ExecuteAsync();

                _allVaultEntries = [.. vault.Entries];
                _allVaultCategories = [.. vault.Categories];

                ReloadVaultData();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshVault()
        {
            if (IsBusy) return;
            await LoadVaultAsync();
            ReloadVaultData();
        }

        #region Synchronous methods

        private void ReloadVaultData()
        {
            if (_sessionContext.Vault != null)
            {
                _allVaultEntries = [.. _sessionContext.Vault.Entries];
                _allVaultCategories = [.. _sessionContext.Vault.Categories];
            }

            UpdateCollection(Passwords, _allVaultEntries);
            UpdateCollection(Categories, _allVaultCategories);

            // TODO: Old pre-refactor code
            // Passwords = _allVaultEntries.ToObservableCollection();
            // Categories = _allVaultCategories.ToObservableCollection();

            PresortEntries(true);
            PresortCategories(true);
        }

        private void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T>? newItems)
        {
            collection.Clear();

            if (newItems == null)
                return;

            foreach (var item in newItems)
                collection.Add(item);
        }

        private void PresortCategories(bool matchUi = false)
        {
            if (!matchUi) _categoriesSortAsc = !_categoriesSortAsc;

            var sortedCategories = VaultFilterService.FilterAndSortCategories(
                _allVaultCategories,
                SearchText,
                _categoriesSortAsc);

            UpdateCollection(Categories, sortedCategories);
        }

        private void PresortEntries(bool matchUi = false)
        {
            if (!matchUi) _passwordsSortAsc = !_passwordsSortAsc;

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
                var filteredCategories = VaultFilterService.FilterAndSortCategories(
                    _allVaultCategories, // Filter from ALL
                    SearchText,
                    _categoriesSortAsc);

                UpdateCollection(Categories, filteredCategories);

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

        #region TOTP

        partial void OnSelectedEntryChanged(VaultEntry? value) => _totpManagerService.TrackEntry(value);

        [RelayCommand]
        private async Task CopyTotpCode() => await _totpManagerService.CopyTotpCodeAsync(SelectedEntry);

        #endregion

        #region Goat Comments

        public void StartRandomGoatComments()
        {
            using var _ = GoatTipsService.StartTips();
            GoatTipsService.PropertyChanged += OnGoatTipsPropertyChanged;
        }

        private void OnGoatTipsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GoatTipsService.CurrentTip):
                    if (GoatTipsService.CurrentTip != null)
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
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task Save()
        {
            if (!CanSync()) return;

            IsSyncing = true;
            try
            {
                await _syncingService.Sync();
            }
            finally
            {
                IsSyncing = false;
            }
        }


        [RelayCommand]
        private Task SortCategories()
        {
            PresortCategories();
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task SortEntries()
        {
            PresortEntries();
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task CreateCategory()
        {
            if (await _categoryManagerService.CreateCategoryAsync(Categories))
                ReloadVaultData();
        }

        [RelayCommand]
        private async Task EditCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;
            if (await _categoryManagerService.EditCategoryAsync(target))
                ReloadVaultData();
        }

        [RelayCommand]
        private async Task DeleteCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;
            if (await _categoryManagerService.DeleteCategoryAsync(target))
            {
                SelectedCategory = null;
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task CreateEntry()
        {
            var changed = await _vaultEntryManagerService.CreateEntryAsync(Categories);
            if (changed)
            {
                // TODO: Old pre-refactor code
                // PresortEntries(true);
                ReloadVaultData();
            }
        }

        [RelayCommand]
        private async Task EditEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;
            if (await _vaultEntryManagerService.EditEntryAsync(target, Categories))
                ReloadVaultData();
        }

        [RelayCommand]
        private async Task DeleteEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;
            if (await _vaultEntryManagerService.DeleteEntryAsync(target))
                ReloadVaultData();
        }

        [RelayCommand]
        private async Task CopyEntry() => await VaultEntryManagerService.CopyEntryPasswordAsync(SelectedEntry);

        [RelayCommand]
        private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

        #endregion
    }
}