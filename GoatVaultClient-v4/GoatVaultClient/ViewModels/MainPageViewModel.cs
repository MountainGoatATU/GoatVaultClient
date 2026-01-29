using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Services.Vault;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Core.Extensions;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;

namespace GoatVaultClient.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        #region Properties
        /*
         * Observables
         */
        private List<VaultEntry> _allVaultEntries = new List<VaultEntry>();
        private List<CategoryItem> _allVaultCategories = new List<CategoryItem>();
        [ObservableProperty]
        public ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty]
        public ObservableCollection<VaultEntry> passwords = [];
        [ObservableProperty]
        private CategoryItem selectedCategory = null;
        [ObservableProperty]
        private VaultEntry selectedEntry = null;
        [ObservableProperty]
        private CategoryItem newEntrySelectedCategory;

        [ObservableProperty]
        private string searchText = null;

        private bool _categoriesSortAsc = true;

        private bool _passwordsSortAsc = true;

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        //Dependency Injection
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly IDialogService _dialogService;
        #endregion
        public MainPageViewModel(VaultService vaultService, HttpService httpService, UserService userService, VaultSessionService vaultSessionService, FakeDataSource fakeDataSource, IDialogService dialogService)
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
                // Seeding Categories
                Categories.Clear();
                _allVaultCategories = _fakeDataSource.GetFolderItems();
                // Adding Default Category
                _allVaultCategories.Add(new CategoryItem { Name = "All" });
                // Adding private list to observable collection
                Categories = _allVaultCategories.ToObservableCollection<CategoryItem>();
                PresortCategories(true);

                // Seeding Passwords
                Passwords.Clear();
                _allVaultEntries = _fakeDataSource.GetVaultEntryItems(10);
                Passwords = _allVaultEntries.ToObservableCollection<VaultEntry>();
                PresortEntries(true);
            }

            if (_vaultSessionService.DecryptedVault != null)
            {
                if (_vaultSessionService.DecryptedVault.Categories != null)
                {
                    //Reload Categories
                    Categories.Clear();
                    _allVaultCategories.Add(new CategoryItem { Name = "All" });
                    _allVaultCategories = _vaultSessionService.DecryptedVault.Categories
                        .Select(c => new CategoryItem { Name = c })
                        .ToList();
                    Categories = _allVaultCategories.ToObservableCollection<CategoryItem>();
                    PresortCategories(true);
                }
                if (_vaultSessionService.DecryptedVault.Entries != null)
                {
                    // Reload Passwords
                    Passwords.Clear();
                    _allVaultEntries = _vaultSessionService.DecryptedVault.Entries;
                    Passwords = _allVaultEntries.ToObservableCollection<VaultEntry>();
                    PresortEntries(true);
                }
            }
        }

        private void PresortCategories(bool matchUI = false)
        {
            // Toggle sort
            // matchUI indicates whether to keep the current sort order (true) or toggle it (false)
            // if the button is clicked, we want to toggle the sort order
            // if the method is called from filtering, we want to keep the current sort order
            if (!matchUI)
            {
                _categoriesSortAsc = !_categoriesSortAsc;
            }
            var sorted = _categoriesSortAsc
            ? Categories.OrderBy(f => f.Name)
            : Categories.OrderByDescending(f => f.Name);
            Categories = sorted.ToObservableCollection<CategoryItem>();
        }
        private void PresortEntries(bool matchUI = false)
        {
            // Toggle sort
            // matchUI indicates whether to keep the current sort order (true) or toggle it (false)
            // if the button is clicked, we want to toggle the sort order
            // if the method is called from filtering, we want to keep the current sort orders
            if (!matchUI)
            {
                _passwordsSortAsc = !_passwordsSortAsc;
            }
            var sorted = _passwordsSortAsc
                ? Passwords.OrderBy(f => f.Site)
                : Passwords.OrderByDescending(f => f.Site);

            Passwords = sorted.ToObservableCollection<VaultEntry>();
        }

        partial void OnSelectedCategoryChanged(CategoryItem value)
        {
            SearchText = "";
            if (value.Name == "All")
            {
                Passwords = _allVaultEntries.ToObservableCollection<VaultEntry>();
                PresortEntries(true);
            }
            else
            {
                Passwords = _allVaultEntries
                    .Where(x => x.Category == value.Name)
                    .ToObservableCollection<VaultEntry>();
                PresortEntries(true);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                Passwords = _allVaultEntries
                    .Where(x => x.Site != null && x.Site.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection<VaultEntry>();
                Categories = _allVaultCategories
                    .Where(x => x.Name != null && x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection<CategoryItem>();
            } 
            else
            {
                Passwords = _allVaultEntries.ToObservableCollection<VaultEntry>();
                Categories = _allVaultCategories.ToObservableCollection<CategoryItem>();
            }
            PresortCategories(true);
            PresortEntries(true);
        }

        #endregion
        #region Commands
        /*
         * Categories Commands
         */
        [RelayCommand]
        public async Task SortCategories()
        {
            PresortCategories();
        }

        [RelayCommand]
        public async Task CreateCategory()
        {
            var popup = new GoatVaultClient.Controls.Popups.AddCategoryPopup();

            await MopupService.Instance.PushAsync(popup);
            string? result = await popup.WaitForScan();

            if (result != null)
            {
                bool exists = Categories.Any(c => c.Name.Equals(result, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    CategoryItem temp = new CategoryItem { Name = result };
                    Categories.Add(temp);
                }
                else
                {
                    // Implement Error dialog or toast
                }
            }
        }

        [RelayCommand]
        public async Task EditCategory(CategoryItem category)
        {
            var editingCategory = new CategoryItem
            { Name = category.Name };

            var result = await _dialogService.DisplayFormViewAsync("Edit Category", editingCategory);

            if (result != null)
            {
                var entries = Passwords.Where(e => e.Category == category.Name);
                foreach (var e in entries)
                {
                    e.Category = result.Name;
                }

                category.Name = result.Name;
            }
        }

        [RelayCommand]
        private async Task DeleteCategory(CategoryItem category)
        {
            var result = await _dialogService.ConfirmAsync($"Are you sure you want to delete category \"{category.Name}\"? All entries under this category will also be deleted.", "Confirm Delete", "Delete", "Cancel");

            if (result)
            {
                Categories.Remove(category);
            }
        }
        [RelayCommand]
        public async Task SortEntries()
        {
            PresortEntries();
        }

        [RelayCommand]
        public async Task CopyEntry(string password)
        {
            if (selectedEntry == null)
                return;

            // Copy to clipboard
            await Clipboard.Default.SetTextAsync(selectedEntry.Password);

            await Task.Delay(10000); // 10 seconds
            await Clipboard.Default.SetTextAsync(""); // Clear clipboard
        }

    [RelayCommand]
    public async Task CreateEntry()
    {
        // Populate the list of category names from your ViewModel
        var categoriesList = Categories.ToList();

        var formModel = new VaultEntryForm(categoriesList)
        {
            // Optional: Set a default selected category
            Category = Categories.FirstOrDefault()?.Name ?? throw new NullReferenceException()
        };

            // 2. Show the Auto-Generated Dialog
            // UraniumUI reads the [Selectable] attribute and renders a Picker using AvailableCategories
            var dialog = new GoatVaultClient.Controls.Popups.VaultEntryDialog(formModel);

            await MopupService.Instance.PushAsync(dialog);
            var isSaved = await dialog.WaitForScan();

            if (isSaved != null)
            {
                // Simple validation
                if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
                {
                    return;
                }

                _allVaultEntries.Add(formModel);
            }
            LoadVaultData();
        }

        [RelayCommand]
        private void EditEntry(VaultEntry entry)
        {

        }

        [RelayCommand]
        public async Task DeleteEntry()
        {
            if (selectedEntry == null)
            {
                return;
            }
            var result = await _dialogService.ConfirmAsync($"Delete \"{selectedEntry.Site}\"", $"Are you sure you want to remove password for \"{selectedEntry.Site}\"?");
            if (result)
            {
                Passwords.Remove(selectedEntry);
            }
        }
        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }
    }
    #endregion

    public class EyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = (bool)value;
            return isVisible ? MaterialRounded.Visibility_off : MaterialRounded.Visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
