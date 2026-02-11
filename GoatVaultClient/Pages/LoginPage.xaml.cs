using GoatVaultClient.Controls.Popups;
using GoatVaultClient.ViewModels;
using Microsoft.Extensions.Logging;
using Mopups.Services;

namespace GoatVaultClient.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel _viewModel;
    private readonly ILogger<LoginPage>? _logger;

    public LoginPage(LoginPageViewModel vm, ILogger<LoginPage>? logger = null)
    {
        InitializeComponent();
        BindingContext = vm;
        _viewModel = vm;
        _logger = logger;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            try
            {
                // Initialize connectivity monitoring
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing login page");
                await MopupService.Instance.PushAsync(new PromptPopup(
                    title: "Error",
                    body: "Failed to initialize login page. Please restart the application.",
                    aText: "OK"
                ));
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Unhandled error in LoginPage.OnAppearing");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cleanup connectivity monitoring
        _viewModel.Cleanup();
    }
}
