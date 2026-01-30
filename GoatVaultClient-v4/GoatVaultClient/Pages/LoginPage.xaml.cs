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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //Initialize connectivity monitoring
        _viewModel.Initialize();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        //Cleanup connectivity monitoring
        _viewModel.Cleanup();
    }
}