using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using Microsoft.Maui.Controls;

namespace GoatVaultClient
{
    public partial class MainPage : ContentPage
    {
        private readonly ISyncingService _syncingService;
        private readonly SyncStatusBarViewModel _syncStatusBarViewModel;


        // Constants for configuration
        private const int SYNC_INTERVAL_MINUTES = 5;
        public MainPage(
            MainPageViewModel viewModel, 
            ISyncingService syncingService,
            SyncStatusBarViewModel syncStatusBarViewModel)
        {
            _syncingService = syncingService;
            _syncStatusBarViewModel = syncStatusBarViewModel;

            InitializeComponent();

            // Set main viewmodel
            BindingContext = viewModel;

            // set sync status bar viewmodel
            SyncStatusBar.BindingContext = _syncStatusBarViewModel;

            GoatBubbleStack.Opacity = 0; // Start hidden

            if (BindingContext is MainPageViewModel vm)
            {
                vm.PropertyChanged += async (sender, args) =>
                {
                    if (args.PropertyName != nameof(vm.IsGoatCommentVisible)) 
                        return;

                    // Fade in and out 0.3 s
                    if (vm.IsGoatCommentVisible)
                    {
                        await GoatBubbleStack.FadeToAsync(1, 300);
                    }
                    else
                    {
                        await GoatBubbleStack.FadeToAsync(0, 300);
                    }
                };
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Enable flyout navigation
            ((AppShell)Shell.Current).EnableFlyout();

            // Perform initial sync
            await _syncingService.Sync();

            // Start periodic background sync (every 5 minutes)
            _syncingService.StartPeriodicSync(TimeSpan.FromMinutes(SYNC_INTERVAL_MINUTES));

            // Safely cast the BindingContext and call the method
            if (BindingContext is MainPageViewModel vm)
            {
                vm.LoadVaultData();
                vm.StartRandomGoatComments();
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            // Stop periodic sync when page is not visible
            // This prevents unnecessary background operations
            // Note: For global sync, move this to App.xaml.cs lifecycle
            _syncingService.StopPeriodicSync();
        }

        private void OnCategoriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item == null)
                return;

            // Specifically update the Active List
            CategoriesCollection.SelectedItem = item;

            // Optional: Clear selection in the other list if you want exclusive selection
            CategoriesCollection.SelectedItem = null;
        }

        private void OnEntriesCollectionMenuOpening(object sender, EventArgs e)
        {
            var flyout = sender as MenuFlyout;
            var item = flyout?.BindingContext;

            if (item == null)
                return;

            // Specifically update the Active List
            EntriesCollection.SelectedItem = item;

            // Optional: Clear selection in the other list if you want exclusive selection
            EntriesCollection.SelectedItem = null;
        }
    }
}
