using Microsoft.Extensions.Logging;
using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class SingleInputPopup : PopupPage
{
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly ILogger<SingleInputPopup>? _logger;
    public Task<string?> WaitForScan() => _tcs.Task;
    public string? Title { get; set; }
    public string? InputFieldTitle { get; set; }
    public string? InputFieldText { get; set; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public SingleInputPopup(
        string title = "",
        string inputFieldTitle = "",
        string inputFieldText = "",
        ILogger<SingleInputPopup>? logger = null)
    {
        _logger = logger;

        InitializeComponent();

        Title = title;
        InputFieldTitle = inputFieldTitle;
        InputFieldText = inputFieldText;

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;
    }

    private async void OnAccept()
    {
        try
        {
            _tcs.TrySetResult(InputField.Text);
            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error awaiting MopupService.Instance.PopAsync();");
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
            _logger?.LogError(e, "Error awaiting MopupService.Instance.PopAsync();");
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