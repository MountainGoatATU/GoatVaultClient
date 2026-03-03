using GoatVaultClient.Pages;
using GoatVaultClient.ViewModels;
using GoatVaultClient.ViewModels.Controls;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient;

// TODO: Refactor
public partial class MainPage
{
    private readonly ISyncingService _syncingService;
    private readonly ILogger<MainPage>? _logger;

    // Constants for configuration
    private const int SyncIntervalMinutes = 5;
    public MainPage(
        MainPageViewModel viewModel,
        ISyncingService syncingService,
        SyncStatusBarViewModel syncStatusBarViewModel,
        ILogger<MainPage>? logger = null)
    {
        _syncingService = syncingService;
        _logger = logger;

        InitializeComponent();
        BindingContext = viewModel;

        SyncStatusBar.BindingContext = syncStatusBarViewModel;
        GoatBubbleStack.Opacity = 0; // Start hidden

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
            //if ()

            // Fire-and-forget sync so the page renders immediately (Bug 2 fix)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _syncingService.Sync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Background sync failed during MainPage.OnAppearing");
                }
            });

            _syncingService.StartPeriodicSync(TimeSpan.FromMinutes(SyncIntervalMinutes));

            // Safely cast the BindingContext and call the method
            if (BindingContext is not MainPageViewModel vm)
                return;
            // Clear selection when returning to the main list
            if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            {
                vm.SelectedEntry = null;
            }
            vm.StartRandomGoatComments();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Unhandled error in MainPage.OnAppearing");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop periodic sync when page is not visible
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
