using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class CreateCategoryPopup
{
    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly ILogger<CreateCategoryPopup>? _logger;
    public Task<string?> WaitForScan() => _tcs.Task;
    public new string? Title { get; set; }
    public string? CategoryFieldTitle { get; set; }
    public string? CategoryFieldText { get; set; }
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public CreateCategoryPopup(
        string title = "",
        string inputFieldTitle = "",
        string inputFieldText = "",
        ILogger<CreateCategoryPopup>? logger = null)
    {
        _logger = logger;

        InitializeComponent();

        Title = title;
        CategoryFieldTitle = inputFieldTitle;
        CategoryFieldText = inputFieldText;

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;
    }

    private async void OnAccept()
    {
        CategoryField.DisplayValidation();

        if (!CategoryField.IsValid)
            return;

        try
        {
            _tcs.TrySetResult(CategoryField.Text);
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