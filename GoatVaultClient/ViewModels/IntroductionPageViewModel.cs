using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient.ViewModels;

public partial class IntroductionPageViewModel : BaseViewModel
{
    [RelayCommand]
    private static async Task GetStarted()
    {
        // Navigate to Register Page
        await Shell.Current.GoToAsync("//login");
    }
}