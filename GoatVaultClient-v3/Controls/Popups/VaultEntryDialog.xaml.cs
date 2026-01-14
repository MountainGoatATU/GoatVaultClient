using Mopups.Services;
using Mopups.Pages;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.Controls.Popups;

public partial class VaultEntryDialog : PopupPage
{
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private TaskCompletionSource<bool> _tcs = new();
    public Task<bool> WaitForScan() => _tcs.Task;

    public VaultEntryDialog(VaultEntryForm vm)
    {
        InitializeComponent();
        BindingContext = vm;

        if (vm.AvailableCategories != null)
        {
            CategoryInput.ItemsSource = vm.AvailableCategories.Select(c => c.Name).ToList();
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        await MopupService.Instance.PopAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(true);
        await MopupService.Instance.PopAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // If the dialog closes for ANY reason (background click, back button), 
        // ensure we cancel the task if it hasn't been completed yet.
        _tcs.TrySetResult(false);
    }
}