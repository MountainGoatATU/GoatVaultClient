using System.Windows.Input;
using Mopups.Services;
using Mopups.Pages;
using GoatVaultCore.Models.Vault;

namespace GoatVaultClient.Controls.Popups;

public partial class VaultEntryDialog : PopupPage
{
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private readonly TaskCompletionSource<VaultEntryForm?> _tcs = new();
    public Task<VaultEntryForm?> WaitForScan() => _tcs.Task;
    public VaultEntryForm ViewModel { get; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    public VaultEntryDialog(VaultEntryForm vm/*, string title = "Create New Password"*/)
    {
        InitializeComponent();
        ViewModel = vm;

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        CategoryInput.ItemsSource = vm.AvailableCategories.ConvertAll(c => c.Name);

        BindingContext = this;
    }

    private async void OnAccept()
    {
        try
        {
            _tcs.TrySetResult(ViewModel);
            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    private async void OnCancel()
    {
        try
        {
            _tcs.TrySetResult(null);
            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // If the dialog closes for ANY reason (background click, back button), 
        // ensure we cancel the task if it hasn't been completed yet.
        _tcs.TrySetResult(null);
    }
}