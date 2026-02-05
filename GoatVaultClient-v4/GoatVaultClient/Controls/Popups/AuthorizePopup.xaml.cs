using Mopups.Pages;
using Mopups.Services;
using System.ComponentModel;
using System.Windows.Input;
using UraniumUI.Material.Attachments;
using UraniumUI.Material.Controls;

namespace GoatVaultClient.Controls.Popups;

public partial class AuthorizePopup : PopupPage, INotifyPropertyChanged
{
    public string title = "Authorization";
    public string Title { get; set; }

    private readonly TaskCompletionSource<string?> _tcs = new();
    private bool _resultSet = false;

    public Task<string?> WaitForScan()
    {
        System.Diagnostics.Debug.WriteLine("WaitForScan called - returning task");
        return _tcs.Task;
    }

    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    public AuthorizePopup(string title, bool isPassword = true)
    {
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine($"AuthorizePopup Constructor - Title: '{title}', IsPassword: {isPassword}");

        Title = title;

        if (InputEntry != null)
        {
            InputEntry.IsPassword = isPassword;

            // Only add password show/hide attachment if it's a password field
            if (isPassword)
            {
                var showHideAttachment = new TextFieldPasswordShowHideAttachment();
                InputEntry.Attachments.Add(showHideAttachment);
                System.Diagnostics.Debug.WriteLine("Added password show/hide attachment");
            }
            else
            {
                // Set appropriate keyboard for non-password fields
                // You can customize this further if needed
                InputEntry.Keyboard = Keyboard.Default;
                System.Diagnostics.Debug.WriteLine("Non-password field, using default keyboard");
            }
        }

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;

        System.Diagnostics.Debug.WriteLine($"AuthorizePopup initialized - InputEntry.Text: '{InputEntry?.Text}'");
    }

    private async void OnAccept()
    {
        System.Diagnostics.Debug.WriteLine($"OnAccept called - InputEntry.Text: '{InputEntry?.Text}'");
        if (!_resultSet)
        {
            _resultSet = true;
            var result = InputEntry?.Text ?? string.Empty;
            _tcs.TrySetResult(result);
            System.Diagnostics.Debug.WriteLine($"Result set to: '{result}'");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Result already set, skipping");
        }

        await MopupService.Instance.PopAsync();
        System.Diagnostics.Debug.WriteLine("Popup dismissed after Accept");
    }

    private async void OnCancel()
    {
        System.Diagnostics.Debug.WriteLine("OnCancel called");
        if (!_resultSet)
        {
            _resultSet = true;
            _tcs.TrySetResult(null);
            System.Diagnostics.Debug.WriteLine("Result set to null");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Result already set, skipping");
        }

        await MopupService.Instance.PopAsync();
        System.Diagnostics.Debug.WriteLine("Popup dismissed after Cancel");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        System.Diagnostics.Debug.WriteLine($"OnDisappearing called - _resultSet: {_resultSet}");

        // Only set result if it hasn't been set yet
        if (!_resultSet)
        {
            _resultSet = true;
            _tcs.TrySetResult(null);
            System.Diagnostics.Debug.WriteLine("Result set to null in OnDisappearing");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Result already set in OnDisappearing, skipping");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("OnAppearing called");

        // Focus the input field when popup appears
        if (InputEntry != null)
        {
            InputEntry.Focus();
        }
    }
}
