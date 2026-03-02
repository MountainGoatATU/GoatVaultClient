using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient.ViewModels;

public partial class OnboardingPageViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task GetStarted() 
        => await SafeExecuteAsync(async () 
            => await Shell.Current.GoToAsync("//login"));
}
