using Mopups.Pages;
using Mopups.Services;
using System.ComponentModel;
using System.Windows.Input;

namespace GoatVaultClient.Controls.Popups;

public partial class AuthorizePopup : PopupPage, INotifyPropertyChanged
{
    public string title = "Authorization";
    public string Title { get; set; }

    // This allows the ViewModel to await the result (true = Save, false = Cancel)
    private readonly TaskCompletionSource<string?> _tcs = new();
    public Task<string?> WaitForScan() => _tcs.Task;
    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    public AuthorizePopup(string title, bool isPassword = true)
    {
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine($"AuthorizePopup Constructor - Title: '{title}', IsPassword: {isPassword}");

        Title = title;

        // Set password mode and attachment visibility
        ShowHideAttachment?.IsVisible = isPassword;

        if (InputEntry != null)
        {
            InputEntry.IsPassword = isPassword;
            // Set keyboard type for numeric input when not password
            if (!isPassword)
            {
                InputEntry.Keyboard = Keyboard.Numeric;
            }
        }

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;

        System.Diagnostics.Debug.WriteLine("AuthorizePopup initialized");
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