using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class GratitudePageViewModel(ILogger<GratitudePageViewModel>? logger = null) : BaseViewModel
{
    [RelayCommand]
    private async Task Continue()
    {
        try
        {
            // Go to Main Page (AppShell Root)
            await Shell.Current.GoToAsync("//main/home");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error navigating to main/home page");
        }
    }
}