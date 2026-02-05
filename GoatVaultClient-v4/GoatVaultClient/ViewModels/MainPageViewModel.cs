using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;
using System.Collections.ObjectModel;
using UraniumUI.Dialogs;
using UraniumUI.Icons.MaterialSymbols;
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
        [ObservableProperty] public ObservableCollection<CategoryItem> categories = [];
        [ObservableProperty] public ObservableCollection<VaultEntry> passwords = [];
        [ObservableProperty] private CategoryItem selectedCategory = null;
        [ObservableProperty] private VaultEntry selectedEntry = null;
        [ObservableProperty] private CategoryItem newEntrySelectedCategory;
        [ObservableProperty] private string searchText = null;
        private bool _categoriesSortAsc = true;
        private bool _passwordsSortAsc = true;
        private bool CanSync() => !IsBusy;
        [ObservableProperty] private bool _isPasswordVisible = false;
        [ObservableProperty] private double vaultScore;
        [ObservableProperty] private string goatComment = "";
        [ObservableProperty] private bool isGoatCommentVisible = false;

        // UI Properties for the Sync Status Component
        [ObservableProperty] private bool _isSyncing = false;

        // TOTP timer
        private IDispatcherTimer? _totpTimer;

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
        private readonly ISyncingService _syncingService;
        #endregion
        public MainPageViewModel(
            VaultService vaultService,
            VaultSessionService vaultSessionService,
            FakeDataSource fakeDataSource,
            IDialogService dialogService,
            ISyncingService syncingService)
        {
            // Dependency Injection
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
            _fakeDataSource = fakeDataSource;
            _dialogService = dialogService;

            LoadVaultData();

            StartTotpTimer();
            StartRandomGoatComments();
        }

        private void StartTotpTimer()
        {
            // Update TOTP codes every second
            _totpTimer = Application.Current?.Dispatcher.CreateTimer();

            if (_totpTimer != null)
            {
                _totpTimer.Interval = TimeSpan.FromSeconds(1);
                _totpTimer.Tick += (s, e) =>
                {
                    UpdateTotpCodes();
                };
                _totpTimer.Start();
            }
        }

        private void UpdateTotpCodes()
        {
            if (SelectedEntry == null || !SelectedEntry.HasMfa ||
                string.IsNullOrWhiteSpace(SelectedEntry.MfaSecret))
            {
                return;
            }

            try
            {
                var (code, secondsRemaining) = TotpService.GenerateCodeWithTime(SelectedEntry.MfaSecret);
                SelectedEntry.CurrentTotpCode = code;
                SelectedEntry.TotpTimeRemaining = secondsRemaining;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating TOTP: {ex.Message}");
                SelectedEntry.CurrentTotpCode = "ERROR";
                SelectedEntry.TotpTimeRemaining = 0;
            }
        }

        partial void OnSelectedEntryChanged(VaultEntry? value)
        {
            // Update TOTP immediately when entry is selected
            if (value is { HasMfa: true } && !string.IsNullOrWhiteSpace(value.MfaSecret))
            {
                UpdateTotpCodes();
            }
        }

        [RelayCommand]
        private async Task CopyTotpCode()
        {
            if (SelectedEntry == null || string.IsNullOrWhiteSpace(SelectedEntry.CurrentTotpCode))
                return;

            await Clipboard.Default.SetTextAsync(SelectedEntry.CurrentTotpCode);
            await Task.Delay(10000); // 10 seconds
            await Clipboard.Default.SetTextAsync(""); // Clear clipboard
        }

        private void StartRandomGoatComments()
            _syncingService = syncingService;
        }

        public void StartRandomGoatComments()
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
            // Reload from decrypted vault
            Passwords = _vaultSessionService.DecryptedVault.Entries.ToObservableCollection();
            Categories = _vaultSessionService.DecryptedVault.Categories.ToObservableCollection();
            // Presort to match UI
            PresortEntries(true);
            PresortCategories(true);
            // Recalculate Vault Score
            CalculateVaultScore();
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
                ReloadVaultData();
            }
            else
            {
                Passwords = Passwords
                    .Where(x => x.Category == value?.Name)
                    .ToObservableCollection();
            }

            PresortEntries(true);
        }

        partial void OnSearchTextChanged(string? value)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                Passwords = Passwords
                    .Where(x => x.Site.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection();
                Categories = Categories
                    .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection();
            }
            else
            {
                ReloadVaultData();
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
        [RelayCommand(CanExecute = nameof(CanSync))]
        private async Task Save()
        {
            // Indicate syncing
            IsSyncing = true;
            // Save to local and server
            await _syncingService.Save(Categories, Passwords, _vaultSessionService.CurrentUser);
            // Reset syncing indicator
            IsSyncing = false;
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
                var exists = Categories.Any(c => c.Name.Equals(result, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    // Create temp Category
                    var temp = new CategoryItem { Name = result };

                    // Add to global list
                    _vaultSessionService.DecryptedVault.Categories.Add(temp);
                  
                    // Update the session vault data
                    _vaultSessionService.DecryptedVault?.Categories = _allVaultCategories
                        .Where(c => c.Name != "All")
                        .Select(c => c.Name)
                        .ToList();

                    // Sort to match UI
                    PresortCategories(true);

                    // Update categories
                    Categories = _allVaultCategories.ToObservableCollection();

                    ReloadVaultData();
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

            // Find the index of the category in the vault
            var categories = _vaultSessionService.DecryptedVault.Categories;
            var index = categories.IndexOf(target);

            if (index < 0)
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
                string oldName = target.Name;
                if (Passwords.Any(c => c.Category == oldName))
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

                    var vaultEntries = _vaultSessionService.DecryptedVault.Entries;
                    foreach (var pwd in vaultEntries.Where(c => c.Category == oldName))
                    {
                        pwd.Category = promptResponse ? response : string.Empty;
                    }
                    category.Name = response;

                    // Update the category name in place using the index
                    _vaultSessionService.DecryptedVault.Categories[index].Name = response;
                }
                ReloadVaultData();
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
                if (Passwords.Any(c => c.Category == category.Name))
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
                    var passwords = Passwords.Where(c => c.Category == target.Name).ToList();
                    if (promptResponse)
                    {
                        foreach (var pwd in passwords)
                        {
                            _vaultSessionService.DecryptedVault.Entries.Remove(pwd);
                        }
                    }
                }
              
                // Remove from UI
                Categories.Remove(target);

                // Remove category
                _vaultSessionService.DecryptedVault.Categories.Remove(target);
              
                // Set selected category
                SelectedCategory = null;
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
            var categoriesList = Categories.ToList();

            var formModel = new VaultEntryForm(categoriesList)
            {
                // Optional: Set a default selected category
                Category = Categories.FirstOrDefault()?.Name ?? throw new NullReferenceException()
            };

            // Show the Auto-Generated Dialog
            var dialog = new VaultEntryDialog(formModel);
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

            var newEntry = new VaultEntry
            {
                UserName = formModel.UserName,
                Site = formModel.Site,
                Password = formModel.Password,
                Description = formModel.Description,
                Category = formModel.Category,
                MfaSecret = formModel.MfaSecret,
                HasMfa = formModel.HasMfa
            };

            // Update session vault data
            _vaultSessionService.DecryptedVault?.Entries = _allVaultEntries;

            // Add to list
            _vaultSessionService.DecryptedVault.Entries.Add(newEntry);

            // Sort to match UI
            PresortEntries(true);

            // Update UI
            Passwords = _allVaultEntries.ToObservableCollection();
            CalculateVaultScore();
            ReloadVaultData();
        }

        [RelayCommand]
        private async Task EditEntry()
        {
            var target = entry ?? SelectedEntry;

            // Safe check
            if (SelectedEntry == null)
                return;

            // Find the index of the entry in the vault
            var entries = _vaultSessionService.DecryptedVault.Entries;
            var index = entries.IndexOf(SelectedEntry);

            if (index < 0)
                return;

            // Get reference to the actual entry in the vault
            var target = entries[index];

            // Populate the list of category names from your ViewModel
            var categoriesList = Categories.ToList();

            // Temp model to hold existing data
            var formModel = new VaultEntryForm(categoriesList)
            {
                UserName = target.UserName,
                Site = target.Site,
                Password = target.Password,
                Description = target.Description,
                Category = target.Category,
                MfaSecret = target.MfaSecret,
                HasMfa = target.HasMfa
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
                    Category = formModel.Category,
                    MfaSecret = formModel.MfaSecret,
                    HasMfa = formModel.HasMfa
                };
            }

            // Update the session vault data
            _vaultSessionService.DecryptedVault?.Entries = _allVaultEntries;

            CalculateVaultScore();

            // Update Selected Entry
            target.UserName = formModel.UserName;
            target.Site = formModel.Site;
            target.Password = formModel.Password;
            target.Description = formModel.Description;
            target.Category = formModel.Category;

            ReloadVaultData();
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
                _vaultSessionService.DecryptedVault.Entries.Remove(target);
            }
          
            // Update UI
            CalculateVaultScore();
            ReloadVaultData();
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }
}