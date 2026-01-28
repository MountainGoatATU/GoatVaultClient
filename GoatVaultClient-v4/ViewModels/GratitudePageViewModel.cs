using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient_v4.ViewModels;

public partial class GratitudePageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task Continue()
    {
        // Go to Main Page (AppShell Root)
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }
}