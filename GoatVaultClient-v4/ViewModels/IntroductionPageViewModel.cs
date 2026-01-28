using CommunityToolkit.Mvvm.Input;
using GoatVaultClient_v4.Pages;

namespace GoatVaultClient_v4.ViewModels;

public partial class IntroductionPageViewModel : BaseViewModel
{
    [RelayCommand]
    private static async Task GetStarted()
    {
        // Navigate to Register Page
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}