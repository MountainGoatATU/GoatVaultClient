using System.ComponentModel;

namespace GoatVaultCore.Abstractions;

public interface ISyncingService : INotifyPropertyChanged
{
    // Methods
    Task Sync();
    Task Save();
    Task AutoSaveIfEnabled();
    void StartPeriodicSync(TimeSpan interval);
    void StopPeriodicSync();

    // Properties
    bool MarkedAsChanged { get; }
    bool HasAutoSave { get; }
    bool HasAutoSync { get; }
    DateTime LastSynced { get; }
    DateTime LastSaved { get; }
    SyncStatus SyncStatus { get; }
    bool IsSyncing { get; }
    string SyncStatusMessage { get; }
    string LastSyncedFormatted { get; }

    // Events
    event EventHandler? SyncStarted;
    event EventHandler? SyncCompleted;
    event EventHandler<SyncFailedEventArgs>? SyncFailed;
    event EventHandler? AuthenticationRequired;
}

public enum SyncStatus
{
    Synced,
    Unsynced,
    Syncing,
    Failed
}

public class SyncFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
    public string ErrorMessage => Exception?.Message ?? "Unknown error";
}