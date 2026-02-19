using Mopups.Pages;

namespace GoatVaultClient.Controls.Popups;

public partial class PendingPopup : PopupPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    public Task<bool> WaitForScan() => _tcs.Task;
    public new string Title { get; set; }
    public PendingPopup(string title)
    {

        Title = title;

        InitializeComponent();

        BindingContext = this;
    }
}