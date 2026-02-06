using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient.ViewModels;

public partial class GratitudePageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task Continue()
    {
        // Go to Main Page (AppShell Root)
        await Shell.Current.GoToAsync("//main/home");
    }
}