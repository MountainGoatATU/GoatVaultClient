using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class IntroductionPageViewModel(ILogger<IntroductionPageViewModel>? logger = null) : BaseViewModel
{
    [RelayCommand]
    private async Task GetStarted()
    {
        try
        {
            // Navigate to Login Page
            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error navigating to login page");
        }
    }
}