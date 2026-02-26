using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Vault;
using GoatVaultClient.Services;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace GoatVaultClient.ViewModels;

public partial class MainPageViewModel : BaseViewModel, IDisposable
{
    #region Dependency Injection

    private readonly LoadVaultUseCase _loadVault;
    private readonly ISessionContext _session;
    private readonly ISyncingService _syncing;
    private readonly TotpManagerService _totpManager;
    private readonly CategoryManagerService _categoryManager;
    private readonly VaultEntryManagerService _vaultEntryManager;
    private readonly ILogger<MainPageViewModel>? _logger;
    // private readonly PwnedPasswordService _pwnedPassword;
    public GoatTipsService GoatTips { get; }

    #endregion

    #region Properties

    // Observables
    [ObservableProperty] private ObservableCollection<CategoryItem> _categories = [];
    [ObservableProperty] private ObservableCollection<VaultEntry> _passwords = [];
    [ObservableProperty] private CategoryItem? _selectedCategory;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private double _vaultScore;
    [ObservableProperty] private string _goatComment = string.Empty;
    [ObservableProperty] private bool _isGoatCommentVisible;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailsPanelVisible))]
    private VaultEntry? _selectedEntry;

    // UI Properties for the Sync Status Component
    private bool _categoriesSortAsc = true;
    private bool _passwordsSortAsc = true;
    private List<VaultEntry> _allVaultEntries = [];
    private List<CategoryItem> _allVaultCategories = [];
    private bool CanSync() => !IsBusy;
    private bool _isUpdatingCollections;

    public bool IsDetailsPanelVisible => SelectedEntry != null;

    #endregion

    public MainPageViewModel(
        LoadVaultUseCase loadVault,
        ISessionContext session,
        ISyncingService syncing,
        TotpManagerService totpManager,
        CategoryManagerService categoryManager,
        VaultEntryManagerService vaultEntryManager,
        GoatTipsService goatTips,
        ILogger<MainPageViewModel>? logger = null/*,
        PwnedPasswordService pwnedPassword*/)
    {
        _loadVault = loadVault;
        _session = session;
        _syncing = syncing;
        _totpManager = totpManager;
        _categoryManager = categoryManager;
        _vaultEntryManager = vaultEntryManager;
        _logger = logger;
        GoatTips = goatTips;
        // _pwnedPassword = pwnedPassword;

        // TODO: Check if logic is the same here
        _session.VaultChanged += OnVaultChanged;
        _syncing.AuthenticationRequired += OnAuthenticationRequired;

        StartRandomGoatComments();
        Task.Run(async () => await InitializeAsync());
    }

    private void OnVaultChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_session.Vault == null)
                return;

            _allVaultEntries = [.. _session.Vault.Entries];
            _allVaultCategories = [.. _session.Vault.Categories];
            SelectedEntry = null; // Clear selection to prevent editing a dead reference
            ReloadVaultData();
        });
    }

    private async void OnAuthenticationRequired(object? sender, EventArgs e)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var mainPage = App.CurrentMainPage;
                if (mainPage is not null)
                {
                    await mainPage.DisplayAlertAsync("Session Expired",
                        "Your password has changed on another device. Please log in again.", "OK");
                }

                await Shell.Current.GoToAsync("//login");
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error displaying authentication alert");
        }
    }

    public void Dispose()
    {
        _session.VaultChanged -= OnVaultChanged;
        _syncing.AuthenticationRequired -= OnAuthenticationRequired;
        GoatTips.PropertyChanged -= OnGoatTipsPropertyChanged;
    }

    private async Task InitializeAsync() => await LoadVaultAsync();

    [RelayCommand]
    private async Task LoadVaultAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            var vault = await _loadVault.ExecuteAsync();

            _allVaultEntries = [.. vault.Entries];
            _allVaultCategories = [.. vault.Categories];

            ReloadVaultData();
        });
    }

    [RelayCommand]
    private async Task RefreshVault() => await LoadVaultAsync();

    #region Synchronous methods

    private void ReloadVaultData()
    {
        if (_isUpdatingCollections)
            return;

        try
        {
            _isUpdatingCollections = true;

            if (_session.Vault != null)
            {
                _allVaultEntries = [.. _session.Vault.Entries];
                _allVaultCategories = [.. _session.Vault.Categories];
            }

            UpdateCollection(Passwords, _allVaultEntries);
            UpdateCollection(Categories, _allVaultCategories);

            // TODO: Old pre-refactor code
            // Passwords = _allVaultEntries.ToObservableCollection();
            // Categories = _allVaultCategories.ToObservableCollection();

            PresortEntries(true);
            PresortCategories(true);
        }
        finally
        {
            _isUpdatingCollections = false;
        }
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
        if (!matchUi)
            _categoriesSortAsc = !_categoriesSortAsc;

        var sortedCategories = VaultFilterService.FilterAndSortCategories(
            _allVaultCategories,
            SearchText,
            _categoriesSortAsc);

        UpdateCollection(Categories, sortedCategories);
    }

    private void PresortEntries(bool matchUi = false)
    {
        if (!matchUi)
            _passwordsSortAsc = !_passwordsSortAsc;

        var sortedEntries = VaultFilterService.FilterAndSortEntries(
            _allVaultEntries,
            SearchText,
            SelectedCategory?.Name == "All" ? null : SelectedCategory?.Name,
            _passwordsSortAsc);

        UpdateCollection(Passwords, sortedEntries);
    }

    partial void OnSelectedCategoryChanged(CategoryItem? value)
    {
        if (_isUpdatingCollections)
            return;
        try
        {
            _isUpdatingCollections = true;

            SearchText = string.Empty;

            var filteredEntries = VaultFilterService.FilterAndSortEntries(
                _allVaultEntries, // Filter from ALL
                SearchText, // Clear search filter
                value?.Name,
                _passwordsSortAsc);

            UpdateCollection(Passwords, filteredEntries);
        }
        finally
        {
            _isUpdatingCollections = false;
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (_isUpdatingCollections)
            return;
        try
        {
            _isUpdatingCollections = true;

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
        }
        finally
        {
            _isUpdatingCollections = false;
            ReloadVaultData();
        }
    }

    #endregion

    #region TOTP

    partial void OnSelectedEntryChanged(VaultEntry? value) => _totpManager.TrackEntry(value);

    [RelayCommand]
    private async Task CopyTotpCode()
    {
        await SafeExecuteAsync(async () =>
        {
            await _totpManager.CopyTotpCodeAsync(SelectedEntry);
        });
    }

    #endregion

    #region Goat Comments

    public void StartRandomGoatComments()
    {
        using var _ = GoatTips.StartTips();
        GoatTips.PropertyChanged += OnGoatTipsPropertyChanged;
    }

    private void OnGoatTipsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GoatTips.CurrentTip):
                if (GoatTips.CurrentTip != null)
                    GoatComment = GoatTips.CurrentTip;
                break;

            case nameof(GoatTips.IsTipVisible):
            case nameof(GoatTips.IsGoatEnabled):
                var show = GoatTips is { IsGoatEnabled: true, IsTipVisible: true };
                IsGoatCommentVisible = show;
                if (!show)
                    GoatComment = string.Empty;
                break;
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void ShowAllPasswords()
    {
        SelectedCategory = null;
        SearchText = string.Empty;
        ReloadVaultData();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!CanSync())
            return;

        // We use SafeExecuteAsync to handle errors, but we also want to show IsSyncing state.
        // SafeExecuteAsync sets IsBusy, which disables CanSync (good).
        await SafeExecuteAsync(async () =>
        {
            IsSyncing = true;
            try
            {
                await _syncing.Sync();
            }
            finally
            {
                IsSyncing = false;
            }
        });
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
        await SafeExecuteAsync(async () =>
        {
            if (await _categoryManager.CreateCategoryAsync(Categories))
                ReloadVaultData();
        });
    }

    [RelayCommand]
    private async Task EditCategory(CategoryItem category)
    {
        await SafeExecuteAsync(async () =>
        {
            var target = category ?? SelectedCategory;
            if (await _categoryManager.EditCategoryAsync(target))
                ReloadVaultData();
        });
    }

    [RelayCommand]
    private async Task DeleteCategory(CategoryItem category)
    {
        await SafeExecuteAsync(async () =>
        {
            var target = category ?? SelectedCategory;
            if (await _categoryManager.DeleteCategoryAsync(target))
            {
                SelectedCategory = null;
                ReloadVaultData();
            }
        });
    }

    [RelayCommand]
    private async Task CreateEntry()
    {
        await SafeExecuteAsync(async () =>
        {
            var changed = await _vaultEntryManager.CreateEntryAsync(Categories);
            if (changed)
            {
                ReloadVaultData();
            }
        });
    }

    [RelayCommand]
    private async Task EditEntry(VaultEntry entry)
    {
        await SafeExecuteAsync(async () =>
        {
            var target = entry ?? SelectedEntry;
            if (await _vaultEntryManager.EditEntryAsync(target, Categories))
                ReloadVaultData();
        });
    }

    [RelayCommand]
    private async Task DeleteEntry(VaultEntry entry)
    {
        await SafeExecuteAsync(async () =>
        {
            var target = entry ?? SelectedEntry;
            if (await _vaultEntryManager.DeleteEntryAsync(target))
                ReloadVaultData();
        });
    }

    [RelayCommand]
    private async Task CopyEntry()
    {
        await SafeExecuteAsync(async () =>
        {
            await VaultEntryManagerService.CopyEntryPasswordAsync(SelectedEntry);
        });
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    #endregion
}