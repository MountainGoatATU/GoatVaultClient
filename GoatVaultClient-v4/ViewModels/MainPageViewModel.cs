using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v4.Models.Vault;
using GoatVaultClient_v4.Services;
using GoatVaultClient_v4.Services.Secrets;
using GoatVaultClient_v4.Services.Vault;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Core.Extensions;
using GoatVaultClient_v4.Pages;
using GoatVaultClient_v4.Services.API;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;

namespace GoatVaultClient_v4.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    #region Properties

    /*
     * Observables
     */

    private List<VaultEntry> _allVaultEntries = [];

    [ObservableProperty] private ObservableCollection<CategoryItem> _categories = [];
    [ObservableProperty] private ObservableCollection<VaultEntry> _passwords = [];
    [ObservableProperty] private CategoryItem? _selectedCategory = null;
    [ObservableProperty] private VaultEntry? _selectedEntry = null;
    [ObservableProperty] private CategoryItem _newEntrySelectedCategory;
    [ObservableProperty] private string? _searchText = null;

    private bool _categoriesSortAsc = true;
    private bool _passwordsSortAsc = true;
    private bool _arePasswordVisible = false;

    public bool IsPasswordVisible
    {
        get => _arePasswordVisible;
        set => SetProperty(ref _arePasswordVisible, value);
    }


    // Dependency Injection
    private readonly VaultSessionService _vaultSessionService;
    private readonly FakeDataSource _fakeDataSource;
    private readonly IDialogService _dialogService;

    #endregion

    public MainPageViewModel(
        VaultService vaultService,
        HttpService httpService,
        UserService userService,
        VaultSessionService vaultSessionService,
        FakeDataSource fakeDataSource,
        IDialogService dialogService
        )
    {
        // Dependency Injection
        _vaultSessionService = vaultSessionService;
        _fakeDataSource = fakeDataSource;
        _dialogService = dialogService;

        LoadVaultData();
    }

    #region Synchronous methods
    public void LoadVaultData()
    {
        if (!Categories.Any() && !Passwords.Any())
        {
            // TEST DATA IF USER NOT LOGGED IN
            Categories = _fakeDataSource.GetFolderItems().ToObservableCollection<CategoryItem>();
            Passwords = _fakeDataSource.GetVaultEntryItems(10).ToObservableCollection<VaultEntry>();
            _allVaultEntries = Passwords.ToList();
        }

        if (_vaultSessionService.DecryptedVault == null) return;

        // Reload Categories
        Categories.Clear();
        Categories = _vaultSessionService.DecryptedVault.Categories
            .Select(c => new CategoryItem { Name = c })
            .ToObservableCollection();

        // Reload Passwords
        _allVaultEntries = _vaultSessionService.DecryptedVault.Entries;

        ApplyFilter();
    }

    // The Filter Logic
    private void ApplyFilter()
    {
        // If no category selected, show all
        // Assuming VaultEntry has a 'Category' property. 
        // If not, you will need to add it to your Model.
        var filtered = SelectedCategory == null
            ? _allVaultEntries
            : _allVaultEntries.Where(x => x.Category == SelectedCategory.Name);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered
                .Where(vaultEntry => vaultEntry.Site
                    .Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        Passwords = new ObservableCollection<VaultEntry>(filtered);
    }

    partial void OnSelectedCategoryChanged(CategoryItem value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
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
        var sorted = _categoriesSortAsc
            ? Categories.OrderBy(f => f.Name)
            : Categories.OrderByDescending(f => f.Name);

        Categories = sorted.ToObservableCollection<CategoryItem>();
        _categoriesSortAsc = !_categoriesSortAsc;
    }

    [RelayCommand]
    public async Task CreateCategory()
    {
        var result = await _dialogService
            .DisplayFormViewAsync("New Category", new CategoryItem());

        if (result != null) Categories.Add(result);
    }

    [RelayCommand]
    public async Task EditCategory(CategoryItem category)
    {
        var editingCategory = new CategoryItem
        { Name = category.Name };

        var result = await _dialogService
            .DisplayFormViewAsync("Edit Category", editingCategory);

        if (result != null)
        {
            var entries = Passwords
                .Where(e => e.Category == category.Name);

            foreach (var e in entries) e.Category = result.Name;
            category.Name = result.Name;
        }
    }

    [RelayCommand]
    private async Task DeleteCategory(CategoryItem category)
    {
        var result = await _dialogService
            .ConfirmAsync(
                $"Are you sure you want to delete category \"{category.Name}\"? All entries under this category will also be deleted.",
                "Confirm Delete",
                "Delete",
                "Cancel");

        if (result) Categories.Remove(category);
    }

    // 5. ADD: Save Command
    //[RelayCommand]
    //private void SaveCategory()
    //{
    //    if (string.IsNullOrWhiteSpace(NewCategoryName))
    //        return;

    //    if (!string.IsNullOrWhiteSpace(_editingCategory.Name))
    //    {
    //        // Update existing category
    //        int index = _vaultSessionService.DecryptedVault.Categories.IndexOf(_editingCategory);
    //        if (index >= 0)
    //            _vaultSessionService.DecryptedVault.Categories[index] = NewCategoryName.Trim();

    //        // Update category in existing entries
    //        foreach (var entry in _vaultSessionService.DecryptedVault.Entries
    //                     .Where(e => e.Category == _editingCategory))
    //        {
    //            entry.Category = NewCategoryName.Trim();
    //        }

    //        _editingCategory = null;
    //    }
    //    else
    //    {
    //        if (_vaultSessionService.CurrentUser != null)
    //        {
    //            // Add new category
    //            _vaultSessionService.DecryptedVault.Categories.Add(NewCategoryName.Trim());
    //        }
    //        else
    //        {
    //            Categories.Add(new CategoryItem { Name = NewCategoryName.Trim() });
    //        }

    //    }

    //    // Refresh the UI list
    //    LoadVaultData();

    //    IsCategoryFormVisible = false;
    //}

    /*
     * Entries Commands
     */

    [RelayCommand]
    private void SortEntries(CategoryItem category)
    {
        ArgumentNullException.ThrowIfNull(category);

        // Toggle sort order
        var sorted = _passwordsSortAsc
            ? Passwords.OrderBy(f => f.Site)
            : Passwords.OrderByDescending(f => f.Site);

        Passwords = sorted.ToObservableCollection<VaultEntry>();
        _passwordsSortAsc = !_passwordsSortAsc;
    }

    [RelayCommand]
    public async Task CopyEntry()
    {
        if (SelectedEntry == null)
            return;

        var password = (SelectedEntry as VaultEntryForm)?.Password ?? SelectedEntry.Password;

        if (string.IsNullOrEmpty(password))
            return;

        // Copy to clipboard
        await Clipboard.Default.SetTextAsync(password);

        await Task.Delay(10000); // 10 seconds
        await Clipboard.Default.SetTextAsync(""); // Clear clipboard
    }

    [RelayCommand]
    public async Task CreateEntry()
    {
        var passwordService = new PasswordStrengthService();

        // Populate the list of category names from your ViewModel
        var categoriesList = Categories.ToList();

        var formModel = new VaultEntryForm(passwordService, categoriesList)
        {
            // Optional: Set a default selected category
            Category = Categories.FirstOrDefault()?.Name ?? throw new NullReferenceException()
        };

        // 2. Show the Auto-Generated Dialog
        // UraniumUI reads the [Selectable] attribute and renders a Picker using AvailableCategories
        var dialog = new Controls.Popups.VaultEntryDialog(formModel);

        await MopupService.Instance.PushAsync(dialog);
        var isSaved = await dialog.WaitForScan();

        if (isSaved)
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
            {
                return;
            }

            Passwords.Add(formModel);
        }
    }

    [RelayCommand]
    private void EditEntry(VaultEntry entry)
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public async Task DeleteEntry()
    {
        if (SelectedEntry == null)
        {
            return;
        }
        var result = await _dialogService
            .ConfirmAsync(
                $"Delete \"{SelectedEntry.Site}\"", $"Are you sure you want to remove password for \"{SelectedEntry.Site}\"?");
        if (result)
        {
            Passwords.Remove(SelectedEntry);
        }
    }

    [RelayCommand]
    private void SaveEntry()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private static async Task GoToEducation()
    {
        // Navigate to Education Page
        await Shell.Current.GoToAsync($"//{nameof(EducationPage)}");
    }
    #endregion
}

public class EyeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = (bool)(value ?? throw new ArgumentNullException(nameof(value)));
        return isVisible ? MaterialRounded.Visibility_off : MaterialRounded.Visibility;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}