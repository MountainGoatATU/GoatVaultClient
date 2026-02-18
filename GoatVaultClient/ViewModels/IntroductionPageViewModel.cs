using CommunityToolkit.Mvvm.Input;
using GoatVaultCore.Services.Shamir;
using Microsoft.Extensions.Logging;
using Xecrets.Slip39;

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