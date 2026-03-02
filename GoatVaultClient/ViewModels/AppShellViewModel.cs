using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;

namespace GoatVaultClient.ViewModels;

public partial class AppShellViewModel(LogoutUseCase logout) : ObservableObject
{
    [RelayCommand]
    private async Task LogoutAsync()
    {
        // Execute the business logic
        await logout.ExecuteAsync();

        // Disable the flyout menu upon logout
        if (Shell.Current is AppShell appShell)
            appShell.DisableFlyout();

        // Clear the navigation stack and return to the intro route
        await Shell.Current.GoToAsync("//login");
    }
}
