using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class OnboardingPageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task GetStarted()
    {
        await SafeExecuteAsync(async () =>
        {
            // Navigate to Login Page
            await Shell.Current.GoToAsync("//login");
        });
    }
}
