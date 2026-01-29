using Mopups.Pages;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

// TODO: Unused class?
public partial class QuestionPopup : PopupPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    public Task<string?> WaitForScan() => _tcs.Task;
    public new required string Title { get; set; }
    public ICommand? AcceptCommand { get; private set; }
    public ICommand? CancelCommand { get; private set; }

    public QuestionPopup()
	{
		InitializeComponent();
	}
}