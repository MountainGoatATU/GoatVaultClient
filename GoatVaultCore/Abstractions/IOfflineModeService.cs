namespace GoatVaultCore.Abstractions;

public interface IOfflineModeService
{
    bool IsOffline { get; }
    bool IsManualOfflineEnabled { get; }
    bool IsConnectivityAvailable { get; }
    event EventHandler<bool>? OfflineModeChanged;
    void SetManualOffline(bool enabled);
}
