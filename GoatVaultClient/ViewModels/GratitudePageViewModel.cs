using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class GratitudePageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task Continue()
    {
        await SafeExecuteAsync(async () =>
        {
            // Go to Login Page
            await Shell.Current.GoToAsync("//login");
        });
    }
}
