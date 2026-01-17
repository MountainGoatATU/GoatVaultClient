using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;
using Mopups.Services;

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
        private ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty]
        private ObservableCollection<VaultEntry> passwords = [];
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

        private bool _isPasswordVisible = false;

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }


        //Dependency Injection
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly IDialogService _dialogService;
        #endregion
        public MainPageViewModel(VaultService vaultService, HttpService httpService, UserService userService, VaultSessionService vaultSessionService, FakeDataSource fakeDataSource, IDialogService dialogService)
        {
            //Dependency Injection
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
            
            if (_vaultSessionService.DecryptedVault != null)
            {
                if (_vaultSessionService.DecryptedVault.Categories != null)
                {
                    //Reload Categories
                    Categories.Clear();
                    Categories = _vaultSessionService.DecryptedVault.Categories
                        .Select(c => new CategoryItem { Name = c })
                        .ToObservableCollection();
                }
                if (_vaultSessionService.DecryptedVault.Entries != null)
                {
                    // Reload Passwords
                    _allVaultEntries = _vaultSessionService.DecryptedVault.Entries;

                    ApplyFilter();
                }
            }
        }

        //The Filter Logic
        private void ApplyFilter()
        {
            IEnumerable<VaultEntry> filtered;

            if (SelectedCategory == null)
            {
                // If no category selected, show all
                filtered = _allVaultEntries;
            }
            else
            {
                // Assuming VaultEntry has a 'Category' property. 
                // If not, you will need to add it to your Model.
                filtered = _allVaultEntries.Where(x => x.Category == SelectedCategory.Name);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(x => x.Site != null && x.Site.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
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
            var result = await _dialogService.DisplayFormViewAsync("New Category", new CategoryItem());

            if (result != null)
            {
                Categories.Add(result);
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
                foreach(var e in entries)
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
            // Toggle sort order
            var sorted = _passwordsSortAsc
                ? Passwords.OrderBy(f => f.Site)
                : Passwords.OrderByDescending(f => f.Site);

            Passwords = sorted.ToObservableCollection<VaultEntry>();
            _passwordsSortAsc = !_passwordsSortAsc;
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
            var formModel = new VaultEntryForm
            {
                // Populate the list of category names from your ViewModel
                AvailableCategories = Categories.ToList(),

                // Optional: Set a default selected category
                Category = Categories.FirstOrDefault()?.Name
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
        private void SaveEntry()
        {
            
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private async Task GoToEducation()
        {
            // Navigate to Education Page
            await Shell.Current.GoToAsync($"//{nameof(EducationPage)}");
        }
        #endregion
    }

    public class EyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            return isVisible ? MaterialRounded.Visibility_off : MaterialRounded.Visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
