using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultApplication.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultClient.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    private readonly LogoutUseCase _logoutUseCase;

    public AppShellViewModel(LogoutUseCase logoutUseCase)
    {
        _logoutUseCase = logoutUseCase;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        // Execute the business logic
        await _logoutUseCase.ExecuteAsync();

        // Disable the flyout menu upon logout
        if (Shell.Current is AppShell appShell)
        {
            appShell.DisableFlyout();
        }

        // Clear the navigation stack and return to the intro route
        await Shell.Current.GoToAsync("//login");
    }
}
