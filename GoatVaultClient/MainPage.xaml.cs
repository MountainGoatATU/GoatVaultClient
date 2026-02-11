using GoatVaultClient.Services;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.controls;
using Microsoft.Maui.Controls;

namespace GoatVaultClient
{
    public partial class MainPage : ContentPage
    {
        private readonly ISyncingService _syncingService;


        // Constants for configuration
        private const int SyncIntervalMinutes = 5;
        public MainPage(
            MainPageViewModel viewModel,
            ISyncingService syncingService,
            SyncStatusBarViewModel syncStatusBarViewModel)
        {
            _syncingService = syncingService;

            InitializeComponent();

            // Set main viewmodel
            BindingContext = viewModel;

            // set sync status bar viewmodel
            SyncStatusBar.BindingContext = syncStatusBarViewModel;

            GoatBubbleStack.Opacity = 0; // Start hidden

            // Start hidden until first tip shows
            ApplyVisibility();

            viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName != nameof(MainPageViewModel.IsGoatCommentVisible))
                    return;

                if (BindingContext is not MainPageViewModel vm)
                    return;

                // Fade in and out 0.3 s
                if (vm.IsGoatCommentVisible)
                {
                    await GoatBubbleStack.FadeToAsync(1, 300);
                    await GoatMascot.FadeToAsync(1, 300);
                }
                else
                {
                    await GoatBubbleStack.FadeToAsync(0, 300);
                    await GoatMascot.FadeToAsync(0, 300);
                }
            };
            return;

            void ApplyVisibility()
            {
                var visible = viewModel.IsGoatCommentVisible;
                GoatBubbleStack.Opacity = visible ? 1 : 0;
                GoatMascot.Opacity = visible ? 1 : 0;
            }
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                // Enable flyout navigation
                ((AppShell)Shell.Current).EnableFlyout();

                // Perform initial sync
                await _syncingService.Sync();

                // Start periodic background sync (every 5 minutes)
                _syncingService.StartPeriodicSync(TimeSpan.FromMinutes(SyncIntervalMinutes));

                // Safely cast the BindingContext and call the method
                if (BindingContext is not MainPageViewModel vm)
                    return;

                vm.LoadVaultData();
                vm.StartRandomGoatComments();
            }
            catch (Exception e)
            {
                throw; // TODO handle exception
            }
        }

        protected override void OnDisappearing()
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
