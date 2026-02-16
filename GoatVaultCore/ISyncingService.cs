namespace GoatVaultCore;

public interface ISyncingService
{
    Task SyncAsync();  // One-time sync
    Task SaveAsync();  // Save local changes
    void StartPeriodicSync(TimeSpan interval);
    void StopPeriodicSync();
    event EventHandler? SyncStarted;
    event EventHandler? SyncCompleted;
    event EventHandler? SyncFailed;
}