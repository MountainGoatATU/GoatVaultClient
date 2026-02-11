using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class PromptPopup : PopupPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    public Task<bool> WaitForScan() => _tcs.Task;
    public string Title { get; set; }
    public string Body { get; set; }
    public string AcceptText { get; set; }
    public string CancelText { get; set; }
    public bool ShowCancelButton { get; set; }
    public ICommand? AcceptCommand { get; private set; }
    public ICommand? CancelCommand { get; private set; }

    public PromptPopup(string title, string body, string aText, string? cText = null)
    {
        Title = title;
        Body = body;
        AcceptText = aText;
        CancelText = cText ?? "Cancel";
        ShowCancelButton = !string.IsNullOrEmpty(cText);

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        InitializeComponent();
        BindingContext = this;
    }

    private async void OnAccept()
    {
        _tcs.TrySetResult(true);
        await MopupService.Instance.PopAsync();
    }

    private async void OnCancel()
    {
        _tcs.TrySetResult(false);
        await MopupService.Instance.PopAsync();
    }
}