using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultClient.Services;
using GoatVaultCore.Abstractions;
using Microsoft.Extensions.Logging;

namespace GoatVaultClient.ViewModels;

public partial class SettingsPageViewModel : BaseViewModel
{

    private readonly GoatTipsService _goatTips;
    private readonly IOfflineModeService _offlineMode;
    private readonly ILogger<SettingsPageViewModel>? _logger;


    [ObservableProperty] private bool _goatEnabled;
    [ObservableProperty] private bool _darkModeEnabled;
    [ObservableProperty] private bool _offlineModeEnabled;
    [ObservableProperty] private bool _isConnectivityAvailable;

    public SettingsPageViewModel(GoatTipsService goatTips, IOfflineModeService offlineMode, ILogger<SettingsPageViewModel>? logger = null)
    {
        _goatTips = goatTips;
        _offlineMode = offlineMode;
        _logger = logger;

        GoatEnabled = _goatTips.IsGoatEnabled;

        // Offline mode
        _offlineMode.OfflineModeChanged += OnOfflineModeChanged;
        IsConnectivityAvailable = _offlineMode.IsConnectivityAvailable;

        var savedTheme = Preferences.Get("app_theme", "system");
        DarkModeEnabled = savedTheme == "dark" || (savedTheme == "system" && Application.Current?.RequestedTheme == AppTheme.Dark);
    }

    private void OnOfflineModeChanged(object? sender, bool isOffline)
    {
        OfflineModeEnabled = isOffline;
        IsConnectivityAvailable = _offlineMode.IsConnectivityAvailable;
    }

    partial void OnOfflineModeEnabledChanged(bool value)
    {
        if (!_offlineMode.IsConnectivityAvailable)
            return;

        _offlineMode.SetManualOffline(value);
    }

    partial void OnDarkModeEnabledChanged(bool value)
    {
        Application.Current?.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        Preferences.Set("app_theme", value ? "dark" : "light");
        _logger?.LogInformation("Toggled dark mode");
    }

    partial void OnGoatEnabledChanged(bool value)
    {
        _goatTips.SetEnabled(value);
        _logger?.LogInformation("Toggled goat");
    }
}