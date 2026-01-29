using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Pages;

namespace GoatVaultClient.ViewModels;

public partial class IntroductionPageViewModel : BaseViewModel
{
    [RelayCommand]
    private static async Task GetStarted()
    {
        // Navigate to Register Page
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}