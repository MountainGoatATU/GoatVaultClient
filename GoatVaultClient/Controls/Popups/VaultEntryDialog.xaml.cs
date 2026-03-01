using GoatVaultCore.Models;
using Microsoft.Extensions.Logging;
using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class VaultEntryDialog : PopupPage
{
    private readonly ILogger<VaultEntryDialog>? _logger;
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private readonly TaskCompletionSource<VaultEntryForm?> _tcs = new();
    public Task<VaultEntryForm?> WaitForScan() => _tcs.Task;
    public VaultEntryForm ViewModel { get; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    public VaultEntryDialog(VaultEntryForm vm, ILogger<VaultEntryDialog>? logger = null)
    {
        _logger = logger;

        InitializeComponent();
        ViewModel = vm;

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        
        BindingContext = this;
    }

    private async void OnAccept()
    {
        SiteField.DisplayValidation();
        UsernameField.DisplayValidation();
        PasswordField.DisplayValidation();

        if (!SiteField.IsValid || !UsernameField.IsValid || !PasswordField.IsValid)
        {
            return;
        }

        try
        {
            _tcs.TrySetResult(ViewModel);
            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error during OnAccept() of VaultEntryDialog");
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
            _logger?.LogError(e, "Error during OnCancel() of VaultEntryDialog");
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
