using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoatVaultClient.Services;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class SettingsPageViewModel : BaseViewModel
{

    private readonly GoatTipsService _goatTips;
    private ILogger<SettingsPageViewModel>? _logger;

    [ObservableProperty] private bool goatEnabled;
    [ObservableProperty] private bool darkModeEnabled;

    public SettingsPageViewModel(GoatTipsService goatTips, ILogger<SettingsPageViewModel>? logger = null)
    {
        _goatTips = goatTips;
        _logger = logger;

        GoatEnabled = _goatTips.IsGoatEnabled;

        var savedTheme = Preferences.Get("app_theme", "system");
        DarkModeEnabled = savedTheme == "dark" || (savedTheme == "system" && Application.Current?.RequestedTheme == AppTheme.Dark);

        Application.Current?.UserAppTheme = DarkModeEnabled ? AppTheme.Dark : AppTheme.Light;
    }

    [RelayCommand]
    private void ToggleGoat()
    {
        GoatEnabled = !GoatEnabled;
        _goatTips.SetEnabled(GoatEnabled);
        _logger?.LogInformation("Toggled goat");
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        DarkModeEnabled = !DarkModeEnabled;

        Application.Current?.UserAppTheme = DarkModeEnabled ? AppTheme.Dark : AppTheme.Light;
        Preferences.Set("app_theme", DarkModeEnabled ? "dark" : "light");

        _logger?.LogInformation("Toggled dark mode");
    }
}