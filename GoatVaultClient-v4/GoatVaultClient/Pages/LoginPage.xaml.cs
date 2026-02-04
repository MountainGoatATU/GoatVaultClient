using GoatVaultClient.ViewModels;

namespace GoatVaultClient.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel _viewModel;
    public LoginPage(LoginPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _viewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // Initialize connectivity monitoring
            _viewModel.Initialize();
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            System.Diagnostics.Debug.WriteLine($"Error initializing login page: {ex}");
            await DisplayAlertAsync("Error", "Failed to initialize login page. Please restart the application.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        //Cleanup connectivity monitoring
        _viewModel.Cleanup();
    }
}