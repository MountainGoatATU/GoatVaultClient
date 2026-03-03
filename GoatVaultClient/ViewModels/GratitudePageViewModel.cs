using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient.ViewModels;

public partial class GratitudePageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task Continue() 
        => await SafeExecuteAsync(async () 
            => await Shell.Current.GoToAsync("//login"));
}
