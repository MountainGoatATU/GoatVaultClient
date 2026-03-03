using Microsoft.Extensions.Logging;
using Mopups.Services;
using System.Windows.Input;
using UraniumUI.Material.Controls;

namespace GoatVaultClient.Controls.Popups;

public partial class AuthorizePopup
{
    public static new readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(AuthorizePopup), "Authorization");

    public new string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

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

            // Mark as set so OnDisappearing doesn't auto-cancel
            _resultSet = true;
            var result = InputEntry?.Text ?? string.Empty;

            // Close popup
            await MopupService.Instance.PopAsync();

            // Set result AFTER pop completes
            _tcs.TrySetResult(result);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error in AuthorizePopup.OnAccept");
            // Ensure we don't hang the caller if pop fails
            _tcs.TrySetResult(null);
        }
    }

    private async void OnCancel()
    {
        try
        {
            _logger?.LogTrace("OnCancel called");

            // Mark as set
            _resultSet = true;

            // Close popup
            await MopupService.Instance.PopAsync();

            // Set result (null for cancel)
            _tcs.TrySetResult(null);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error in AuthorizePopup.OnCancel");
            _tcs.TrySetResult(null);
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
