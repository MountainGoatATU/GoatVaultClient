using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;
using GoatVaultCore.Services.Secrets;

namespace GoatVaultClient.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        #region Properties
        /*
         * Observables
         */
        private List<VaultEntry> _allVaultEntries = [];
        private List<CategoryItem> _allVaultCategories = [];
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
        [ObservableProperty] private string goatComment = "";
        [ObservableProperty] private bool isGoatCommentVisible = false;

        // UI Properties for the Sync Status Component
        [ObservableProperty] private bool _isSyncing = false;

        // Random comments
        private readonly List<string> _goatTips =
        [
            "Tip: Change your password regularly!",
            "Tip: Avoid using the same password twice.",
            "Tip: Enable two-factor authentication.",
            "Tip: Your Vault score is low, check weak passwords.",
            "Tip: Keep backup keys handy.",
            "Tip: Consider using a passphrase instead of a single word."
        ];

        // Dependency Injection
        // TODO: Unused parameters
        private readonly VaultSessionService _vaultSessionService;
        private readonly FakeDataSource _fakeDataSource;
        private readonly IDialogService _dialogService;
        private readonly VaultService _vaultService;
        #endregion
        public MainPageViewModel(VaultService vaultService, UserService userService, VaultSessionService vaultSessionService, FakeDataSource fakeDataSource, IDialogService dialogService)
        {
            // Dependency Injection
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
            _fakeDataSource = fakeDataSource;
            _dialogService = dialogService;

            LoadVaultData();

            StartRandomGoatComments();
        }

        private void StartRandomGoatComments()
        {
            var random = new Random();
            var timer = Application.Current.Dispatcher.CreateTimer();

            var counter = 0;
            timer.Interval = TimeSpan.FromSeconds(1);

            timer.Tick += (s, e) =>
            {
                counter++;

                switch (counter % 10)
                {
                    // Every 10 seconds show a comment
                    case 0:
                        GoatComment = _goatTips[random.Next(_goatTips.Count)];
                        IsGoatCommentVisible = true;
                        break;
                    // Disappear after 5 seconds
                    case 5:
                        IsGoatCommentVisible = false;
                        GoatComment = "";
                        break;
                }

                if (counter >= 10) counter = 0; // Reset counter
            };
            timer.Start();
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
         * Sync commands
         */
        // Manual Sync Command
        [RelayCommand]
        private async Task ManualSync()
        {
            if (_vaultSessionService.DecryptedVault == null || _vaultSessionService.CurrentUser == null)
                return;
            if (_allVaultCategories.Count != 0 && _allVaultEntries.Count != 0)
            {
                _vaultSessionService.DecryptedVault.Categories = [.. _allVaultCategories
                    .Where(c => c.Name != "All")
                    .Select(c => c.Name)];
                _vaultSessionService.DecryptedVault.Entries = _allVaultEntries;
            }
            IsBusy = true;
            await _vaultService.SaveVaultAsync(
                _vaultSessionService.CurrentUser,
                _vaultSessionService.MasterPassword,
                _vaultSessionService.DecryptedVault
                );
            IsBusy = false;
        }

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
            var popup = new Controls.Popups.SingleInputPopup("Create Category", "Category Name", "");

            await MopupService.Instance.PushAsync(popup);
            var result = await popup.WaitForScan();

            if (result != null)
            {
                var exists = _allVaultCategories.Any(c => c.Name.Equals(result, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    // Create temp Category
                    var temp = new CategoryItem { Name = result };

                    // Add it to global list
                    _allVaultCategories.Add(temp);

                    // Update the session vault data
                    _vaultSessionService.DecryptedVault?.Categories = _allVaultCategories
                        .Where(c => c.Name != "All")
                        .Select(c => c.Name)
                        .ToList();

                    // Sort to match UI
                    PresortCategories(true);

                    // Update categories
                    Categories = _allVaultCategories.ToObservableCollection();

                    // Auto-save after creating category
                    await ManualSync();
                }
                else
                {
                    // TODO: Implement error dialog or toast
                }
            }
        }

        [RelayCommand]
        public async Task EditCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;
            // Safe check
            if (target == null)
                return;
            // Creating new prompt dialog
            var categoryPopup = new SingleInputPopup("Edit Category", "Category", category.Name);
            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(categoryPopup);
            // Wait for the response
            var response = await categoryPopup.WaitForScan();
            // Act based on the response
            if (response != string.Empty)
            {
                if (Passwords.Any(c => c.Category == category.Name))
                {
                    while (MopupService.Instance.PopupStack.Contains(categoryPopup))
                        await Task.Delay(50);
                    // Asking user to reassign the passwords
                    var promptPopup = new PromptPopup("Reassign Passwords", $"Do you want to reassign passwords from \"{target.Name}\" to \"{response}\"?", "Accept");

                    // Displaying dialog
                    await MopupService.Instance.PushAsync(promptPopup);

                    // Waiting for the response 
                    var promptResponse = await promptPopup.WaitForScan();
                    var passwords = _allVaultEntries.Where(c => c.Category == target.Name).ToList();
                    if (promptResponse)
                    {
                        foreach (var pwd in passwords)
                        {
                            pwd.Category = response;
                        }
                    }
                    else
                    {
                        foreach (var pwd in passwords)
                        {
                            pwd.Category = string.Empty;
                        }
                    }
                    category.Name = response;
                }
            }
        }

        [RelayCommand]
        private async Task DeleteCategory(CategoryItem category)
        {
            var target = category ?? SelectedCategory;

            // Safe check
            if (target == null)
                return;

            // Creating new prompt dialog
            var categoryPopup = new PromptPopup("Confirm Delete", $"Are you sure you want to delete the \"{target.Name}\" category?", "Delete");

            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(categoryPopup);

            // Wait for the response
            var response = await categoryPopup.WaitForScan();

            // Act based on the response
            if (response)
            {
                if (_allVaultEntries.Any(c => c.Category == category.Name))
                {
                    // Wait before pushing another dialog
                    while (MopupService.Instance.PopupStack.Contains(categoryPopup))
                        await Task.Delay(50);

                    // Asking user to delete the passwords assign to the deleting category
                    var promptPopup = new PromptPopup("Delete Passwords", $"Do you want to delete all passwords from \"{category.Name}\"?", "Delete");

                    // Displaying dialog
                    await MopupService.Instance.PushAsync(promptPopup);

                    // Waiting for the response 
                    var promptResponse = await promptPopup.WaitForScan();
                    var passwords = _allVaultEntries.Where(c => c.Category == target.Name).ToList();
                    if (promptResponse)
                    {
                        foreach (var pwd in passwords)
                        {
                            _allVaultEntries.Remove(pwd);
                        }
                    }
                }
                // Remove from UI
                Categories.Remove(target);

                // Remove from list
                _allVaultCategories.Remove(category);

                // Set selected category
                SelectedCategory = _allVaultCategories.Find(c => c.Name == "All");
            }
        }

        [RelayCommand]
        private async Task SortEntries()
        {
            PresortEntries();
        }

        // TODO: Unused method parameter
        [RelayCommand]
        private async Task CopyEntry(/*string password*/)
        {
            await Clipboard.Default.SetTextAsync(SelectedEntry?.Password);  // Copy to clipboard
            await Task.Delay(10000);  // 10 seconds
            await Clipboard.Default.SetTextAsync("");  // Clear clipboard
        }

        [RelayCommand]
        private async Task CreateEntry()
        {
            // Populate the list of category names from your ViewModel
            var categoriesList = _allVaultCategories.ToList();

            var formModel = new VaultEntryForm(categoriesList)
            {
                // Optional: Set a default selected category
                Category = _allVaultCategories.FirstOrDefault()?.Name ?? throw new NullReferenceException()
            };

            // Show the Auto-Generated Dialog
            var dialog = new VaultEntryDialog(formModel);

            await MopupService.Instance.PushAsync(dialog);
            await dialog.WaitForScan();

            // Simple validation
            if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
            {
                return;
            }

            var newEntry = new VaultEntry
            {
                UserName = formModel.UserName,
                Site = formModel.Site,
                Password = formModel.Password,
                Description = formModel.Description,
                Category = formModel.Category
            };

            // Add to local list
            _allVaultEntries.Add(newEntry);

            // Update session vault data
            _vaultSessionService.DecryptedVault?.Entries = _allVaultEntries;

            // Sort to match UI
            PresortEntries(true);

            // Update UI
            Passwords = _allVaultEntries.ToObservableCollection();

            LoadVaultData();
            CalculateVaultScore();

            // Auto-save after creating entry
            await ManualSync();
        }

        [RelayCommand]
        private async Task EditEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;

            // Safe check
            if (target == null)
                return;

            // Populate the list of category names from your ViewModel
            var categoriesList = Categories.ToList();

            // Temp model to hold existing data
            var formModel = new VaultEntryForm(categoriesList)
            {
                UserName = target.UserName,
                Site = target.Site,
                Password = target.Password,
                Description = target.Description,
                Category = target.Category
            };

            // Create the dialog
            var dialog = new VaultEntryDialog(formModel);

            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(dialog);
            await dialog.WaitForScan();

            // Simple validation
            if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
            {
                return;
            }

            // Find and update the entry in the list
            var entryIndex = _allVaultEntries.IndexOf(target);
            if (entryIndex >= 0)
            {
                _allVaultEntries[entryIndex] = new VaultEntry
                {
                    UserName = formModel.UserName,
                    Site = formModel.Site,
                    Password = formModel.Password,
                    Description = formModel.Description,
                    Category = formModel.Category
                };
            }

            // Update the session vault data
            _vaultSessionService.DecryptedVault?.Entries = _allVaultEntries;

            LoadVaultData();
            CalculateVaultScore();

            // Auto-save after editing entry
            await ManualSync();
        }

        [RelayCommand]
        private async Task DeleteEntry(VaultEntry entry)
        {
            var target = entry ?? SelectedEntry;

            // Safe check
            if (target == null)
                return;

            // Creating new prompt dialog
            var dialog = new PromptPopup("Confirm Delete", $"Are you sure you want to delete the password for \"{target.Site}\"?", "Delete");

            // Push the dialog to MopupService
            await MopupService.Instance.PushAsync(dialog);

            // Wait for the response
            var response = await dialog.WaitForScan();

            // Act based on the response
            if (response)
            {
                // Remove from the list
                _allVaultEntries.Remove(target);

                // Update the session vault data
                _vaultSessionService.DecryptedVault?.Entries = _allVaultEntries;

                // Reload the data
                LoadVaultData();
                CalculateVaultScore();

                // Auto-save after deleting entry
                await ManualSync();
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