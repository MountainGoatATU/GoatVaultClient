using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuthorizePopup>? _logger;
    private bool _resultSet = false;

    public Task<string?> WaitForScan()
    {
        _logger?.LogTrace("WaitForScan called");
        return _tcs.Task;
    }

    public ICommand AcceptCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    public AuthorizePopup(string title, bool isPassword = true, ILogger<AuthorizePopup>? logger = null)
    {
        _logger = logger;
        InitializeComponent();

        _logger?.LogTrace("AuthorizePopup created (Title: {Title}, IsPassword: {IsPassword})", title, isPassword);

        Title = title;

        if (InputEntry != null)
        {
            InputEntry.IsPassword = isPassword;

            // Only add password show/hide attachment if it's a password field
            if (isPassword)
            {
                var showHideAttachment = new TextFieldPasswordShowHideAttachment();
                InputEntry.Attachments.Add(showHideAttachment);
            }
            else
            {
                // Set appropriate keyboard for non-password fields
                InputEntry.Keyboard = Keyboard.Default;
            }
        }

        AcceptCommand = new Command(OnAccept);
        CancelCommand = new Command(OnCancel);

        BindingContext = this;
    }

    private async void OnAccept()
    {
        try
        {
            _logger?.LogTrace("OnAccept called");
            if (!_resultSet)
            {
                _resultSet = true;
                var result = InputEntry?.Text ?? string.Empty;
                _tcs.TrySetResult(result);
            }

            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error in AuthorizePopup.OnAccept");
        }
    }

    private async void OnCancel()
    {
        try
        {
            _logger?.LogTrace("OnCancel called");
            if (!_resultSet)
            {
                _resultSet = true;
                _tcs.TrySetResult(null);
            }

            await MopupService.Instance.PopAsync();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error in AuthorizePopup.OnCancel");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Only set result if it hasn't been set yet
        if (_resultSet)
            return;

        _resultSet = true;
        _tcs.TrySetResult(null);
        _logger?.LogTrace("AuthorizePopup disappeared without explicit result");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Focus the input field when popup appears
        InputEntry?.Focus();
    }
}
