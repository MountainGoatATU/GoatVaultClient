using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class AddCategoryPopup : PopupPage
{
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private TaskCompletionSource<string?> _tcs = new();
    public Task<string?> WaitForScan() => _tcs.Task;
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public AddCategoryPopup()
	{
		InitializeComponent();

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;
    }

    private async void OnAccept()
    {
        _tcs.TrySetResult(InputEntry.Text);
        await MopupService.Instance.PopAsync();
    }

    private async void OnCancel()
    {
        _tcs.TrySetResult(null);
        await MopupService.Instance.PopAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // If the dialog closes for ANY reason (background click, back button), 
        // ensure we cancel the task if it hasn't been completed yet.
        _tcs.TrySetResult(null);
    }
}