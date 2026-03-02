namespace GoatVaultClient.Controls.Popups;

public partial class PendingPopup
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