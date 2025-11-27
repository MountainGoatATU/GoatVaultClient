using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GoatVaultClient_v3.ViewModels
{
    public partial class IntroductionPageViewModel : BaseViewModel
    {
        [RelayCommand]
        private async Task GetStarted()
        {
            // Navigate to Register Page
            await Shell.Current.GoToAsync($"//{nameof(RegisterPage)}");
        }
    }
}