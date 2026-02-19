using Microsoft.Extensions.Logging;
using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class PromptPopup : PopupPage
{
    private readonly ILogger<SingleInputPopup>? _logger;
    private readonly TaskCompletionSource<bool> _tcs = new();
    public Task<bool> WaitForScan() => _tcs.Task;
    public string PopupTitle { get; set; }
    public string Body { get; set; }
    public string AcceptText { get; set; }
    public string CancelText { get; set; }
    public bool ShowCancelButton { get; set; }
    public ICommand? AcceptCommand { get; private set; }
    public ICommand? CancelCommand { get; private set; }

    public PromptPopup(string popupTitle, string body, string aText, string? cText = null, ILogger<SingleInputPopup>? logger = null)
    {
        _logger = logger;

        PopupTitle = popupTitle;
        Body = body;
        AcceptText = aText;
        CancelText = cText ?? "Cancel";
        ShowCancelButton = !string.IsNullOrEmpty(cText);

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        InitializeComponent();
        BindingContext = this;
    }

    private bool _resultSet = false;

    private async void OnAccept()
    {
        try
        {
            if (_resultSet)
                return;
            _resultSet = true;

            try
            {
                await MopupService.Instance.PopAsync();
                _tcs.TrySetResult(true);
            }
            catch
            {
                _tcs.TrySetResult(false);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error during OnAccept() of PromptPopup");
        }
    }

    private async void OnCancel()
    {
        try
        {
            if (_resultSet)
                return;
            _resultSet = true;

            try
            {
                await MopupService.Instance.PopAsync();
                _tcs.TrySetResult(false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error awaiting MopupService.Instance.PopAsync() during OnCancel() of PromptPopup");
                _tcs.TrySetResult(false);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error during OnCancel() of PromptPopup");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_resultSet)
            return;

        _resultSet = true;
        _tcs.TrySetResult(false);
    }
}