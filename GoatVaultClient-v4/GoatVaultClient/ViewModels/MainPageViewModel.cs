using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Maui.Core.Extensions;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Vault;
using GoatVaultInfrastructure.Services.API;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Services.Secrets;

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
        [ObservableProperty] public ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty] public ObservableCollection<VaultEntry> passwords = [];
        [ObservableProperty] private CategoryItem selectedCategory = null;
        [ObservableProperty] private VaultEntry selectedEntry = null;
        [ObservableProperty] private CategoryItem newEntrySelectedCategory;
        [ObservableProperty] private string searchText = null;
        private bool _categoriesSortAsc = true;
        private bool _passwordsSortAsc = true;
        [ObservableProperty] private bool _isPasswordVisible = false;
        [ObservableProperty] private double vaultScore;

        //Dependency Injection
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly IDialogService _dialogService;
        #endregion
        public MainPageViewModel(VaultService vaultService, UserService userService, VaultSessionService vaultSessionService, FakeDataSource fakeDataSource, IDialogService dialogService)
        {
            //Dependency Injection
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
            _fakeDataSource = fakeDataSource;
            _dialogService = dialogService;

            LoadVaultData();
        }
        #region Async methods
        private async void InitializeAsync()
        {

        }
        #endregion
        #region Synchronous methods
        public void LoadVaultData()
        {
            if (!Categories.Any() && !Passwords.Any())
            {
                // TODO: TEST DATA IF USER NOT LOGGED IN
                // Seeding Categories
                Categories.Clear();
                _allVaultCategories = _fakeDataSource.GetFolderItems();
                // Adding Default Category
                _allVaultCategories.Add(new CategoryItem { Name = "All" });
                // Adding private list to observable collection
                Categories = _allVaultCategories.ToObservableCollection();
                PresortCategories(true);

                // Seeding Passwords
                Passwords.Clear();
                _allVaultEntries = _fakeDataSource.GetVaultEntryItems(10);
                Passwords = _allVaultEntries.ToObservableCollection();
                PresortEntries(true);
            }

            if (_vaultSessionService.DecryptedVault == null)
                return;

            // Reload Categories
            Categories.Clear();
            _allVaultCategories.Add(new CategoryItem { Name = "All" });
            _allVaultCategories = _vaultSessionService.DecryptedVault.Categories
                    .ConvertAll(c => new CategoryItem { Name = c })
                ;
            Categories = _allVaultCategories.ToObservableCollection();
            PresortCategories(true);

            // Reload Passwords
            Passwords.Clear();
            _allVaultEntries = _vaultSessionService.DecryptedVault.Entries;
            Passwords = _allVaultEntries.ToObservableCollection();
            PresortEntries(true);
        }

        private void PresortCategories(bool matchUi = false)
        {
            // Toggle sort - matchUI indicates whether to keep the current sort order (true) or toggle it (false)
            // If the button is clicked, we want to toggle the sort order
            // If the method is called from filtering, we want to keep the current sort order
            if (!matchUi)
            {
                _categoriesSortAsc = !_categoriesSortAsc;
            }
            var sorted = _categoriesSortAsc
                ? Categories.OrderBy(f => f.Name)
                : Categories.OrderByDescending(f => f.Name);
            Categories = sorted.ToObservableCollection<CategoryItem>();
        }

        private void PresortEntries(bool matchUi = false)
        {
            // Toggle sort
            // matchUI indicates whether to keep the current sort order (true) or toggle it (false)
            // if the button is clicked, we want to toggle the sort order
            // if the method is called from filtering, we want to keep the current sort orders
            if (!matchUi)
            {
                _passwordsSortAsc = !_passwordsSortAsc;
            }
            var sorted = _passwordsSortAsc
                ? Passwords.OrderBy(f => f.Site)
                : Passwords.OrderByDescending(f => f.Site);

            Passwords = sorted.ToObservableCollection();
        }

        partial void OnSelectedCategoryChanged(CategoryItem? value)
        {
            SearchText = "";
            if (value?.Name == "All")
            {
                Passwords = _allVaultEntries.ToObservableCollection();
            }
            else
            {
                Passwords = _allVaultEntries
                    .Where(x => x.Category == value?.Name)
                    .ToObservableCollection();
            }

            PresortEntries(true);
        }

        partial void OnSearchTextChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                Passwords = _allVaultEntries
                    .Where(x => x.Site.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection();
                Categories = _allVaultCategories
                    .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection();
            }
            else
            {
                Passwords = _allVaultEntries.ToObservableCollection();
                Categories = _allVaultCategories.ToObservableCollection();
            }
            PresortCategories(true);
            PresortEntries(true);
        }

        private void CalculateVaultScore()
        {
            VaultScore = VaultScoreCalculatorService.CalculateScore(Passwords);
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
            var popup = new Controls.Popups.AddCategoryPopup();

            await MopupService.Instance.PushAsync(popup);
            var result = await popup.WaitForScan();

            if (result != null)
            {
                var exists = Categories.Any(c => c.Name.Equals(result, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    var temp = new CategoryItem { Name = result };
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

        // TODO: Unused method parameter
        [RelayCommand]
        public async Task CopyEntry(/*string password*/)
        {
            await Clipboard.Default.SetTextAsync(SelectedEntry?.Password);  // Copy to clipboard
            await Task.Delay(10000);  // 10 seconds
            await Clipboard.Default.SetTextAsync("");  // Clear clipboard
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
            var dialog = new Controls.Popups.VaultEntryDialog(formModel);

            await MopupService.Instance.PushAsync(dialog);
            await dialog.WaitForScan();

            // Simple validation
            if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
            {
                return;
            }

            _allVaultEntries.Add(formModel);
            LoadVaultData();
            CalculateVaultScore();
        }

        [RelayCommand]
        private async Task EditEntry()
        {
            // Safe check
            if (SelectedEntry == null)
                return;
            // Populate the list of category names from your ViewModel
            var categoriesList = Categories.ToList();
            // Temp model to hold existing data
            var formModel = new VaultEntryForm(categoriesList)
            {
                UserName = SelectedEntry.UserName,
                Site = SelectedEntry.Site,
                Password = SelectedEntry.Password,
                Description = SelectedEntry.Description,
                Category = SelectedEntry.Category
            };
            // Create the dialog
            var dialog = new Controls.Popups.VaultEntryDialog(formModel);
            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(dialog);
            await dialog.WaitForScan();
            // Simple validation
            if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
            {
                return;
            }
            // Update Selected Entry
            SelectedEntry = new VaultEntry
            {
                UserName = formModel.UserName,
                Site = formModel.Site,
                Password = formModel.Password,
                Description = formModel.Description,
                Category = formModel.Category
            };
            LoadVaultData();
            CalculateVaultScore();
        }
        [RelayCommand]
        public async Task DeleteEntry()
        {
            // Safe check
            if (SelectedEntry == null)
                return;
            // Creating new prompt dialog
            var dialog = new PromptPopup("Confirm Delete", $"Are you sure you want to delete the password for \"{selectedEntry.Site}\"?", "Delete");
            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(dialog);
            // Wait for the response
            var response = await dialog.WaitForScan();
            // Act based on the response
            if (response)
            {
                Passwords.Remove(SelectedEntry);
                CalculateVaultScore();
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }

    public class EyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? MaterialRounded.Visibility_off : MaterialRounded.Visibility;
            }
            return MaterialRounded.Visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}