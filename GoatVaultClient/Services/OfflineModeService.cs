using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Abstractions;
using Microsoft.Extensions.Logging;
using Mopups.Services;

namespace GoatVaultClient.Services;

public sealed class OfflineModeService : IOfflineModeService, IDisposable
{
    private const string ManualOfflineKey = "ManualOfflineEnabled";
    private readonly ConnectivityService _connectivity;
    private readonly ILogger<OfflineModeService>? _logger;
    private bool _isDisposed;

    public OfflineModeService(
        ConnectivityService connectivity,
        ILogger<OfflineModeService>? logger = null)
    {
        _connectivity = connectivity;
        _logger = logger;

        IsManualOfflineEnabled = Preferences.Default.Get(ManualOfflineKey, false);
        IsOffline = IsManualOfflineEnabled || !_connectivity.GetNetworkInfo().IsConnected;

        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsOffline { get; private set; }
    public bool IsManualOfflineEnabled { get; private set; }
    public bool IsConnectivityAvailable => _connectivity.GetNetworkInfo().IsConnected;

    public event EventHandler<bool>? OfflineModeChanged;

    public void SetManualOffline(bool enabled)
    {
        if (IsManualOfflineEnabled == enabled)
        {
            return;
        }

        IsManualOfflineEnabled = enabled;
        Preferences.Default.Set(ManualOfflineKey, enabled);

        UpdateOfflineState();
    }

    private void OnConnectivityChanged(object? sender, bool isConnected) => UpdateOfflineState();

    private void UpdateOfflineState()
    {
        var shouldBeOffline = IsManualOfflineEnabled || !_connectivity.GetNetworkInfo().IsConnected;
        if (IsOffline == shouldBeOffline)
            return;

        if (shouldBeOffline)
        {
            MopupService.Instance.PushAsync(new ConfirmPopup("Offline Mode", "You are offline. All your changes will be saved to local datbase. Sync is not available", "OK"));
        } else
        {
            MopupService.Instance.PushAsync(new ConfirmPopup("Online Mode", "You are back online. All your changes are being synced with the server", "OK"));
        }

        IsOffline = shouldBeOffline;
        _logger?.LogInformation("Offline mode changed: {IsOffline}", IsOffline);
        OfflineModeChanged?.Invoke(this, IsOffline);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _isDisposed = true;
    }
}
