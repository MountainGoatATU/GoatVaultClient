using GoatVaultCore.Abstractions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GoatVaultClient.ViewModels.Controls;

public class SyncStatusBarViewModel : INotifyPropertyChanged
{
    private readonly ISyncingService _syncingService;
    private readonly Timer _updateTimer;
    private readonly ILogger<SyncStatusBarViewModel>? _logger;

    public SyncStatusBarViewModel(ISyncingService syncingService, ILogger<SyncStatusBarViewModel>? logger = null)
    {
        _syncingService = syncingService;
        _logger = logger;

        // Subscribe to syncing service events
        _syncingService.PropertyChanged += OnSyncingServicePropertyChanged;
        _syncingService.SyncStarted += OnSyncStarted;
        _syncingService.SyncCompleted += OnSyncCompleted;
        _syncingService.SyncFailed += OnSyncFailed;

        // Initialize commands
        SyncCommand = new Command(async void () =>
        {
            try
            {
                await ExecuteSyncCommand();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error awaiting execute sync command");
            }
        }, CanExecuteSync);

        // Start a timer to update the "Last synced" time display every minute
        _updateTimer = new Timer(
            _ => OnPropertyChanged(nameof(LastSyncedFormatted)),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    #region Commands

    public ICommand SyncCommand { get; }

    private bool CanExecuteSync() => !_syncingService.IsSyncing;

    private async Task ExecuteSyncCommand()
    {
        try
        {
            await _syncingService.Sync();
        }
        catch
        {
            // Error is already handled in the service
            // Could show a toast notification here if desired
        }
    }

    #endregion

    #region Properties

    public bool IsSyncing => _syncingService.IsSyncing;
    public bool IsSynced => _syncingService.SyncStatus == SyncStatus.Synced;
    public bool SyncFailed => _syncingService.SyncStatus == SyncStatus.Failed;
    public string SyncStatusText => _syncingService.SyncStatusMessage;
    public string LastSyncedFormatted => _syncingService.LastSyncedFormatted;
    public static bool ShowSyncButton => true; // Always show, but can be controlled by settings
    public bool IsSyncButtonEnabled => !_syncingService.IsSyncing;
    public static bool ShowSyncStatus => true;
    public bool ShowLastUpdated => _syncingService.LastSynced != default;
    public bool IsAutoSyncDisabled => !_syncingService.HasAutoSync;

    #endregion

    #region Event Handlers

    private void OnSyncingServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward relevant property changes to update UI
        switch (e.PropertyName)
        {
            case nameof(ISyncingService.IsSyncing):
                OnPropertyChanged(nameof(IsSyncing));
                OnPropertyChanged(nameof(IsSyncButtonEnabled));
                ((Command)SyncCommand).ChangeCanExecute();
                break;

            case nameof(ISyncingService.SyncStatus):
                OnPropertyChanged(nameof(IsSynced));
                OnPropertyChanged(nameof(SyncFailed));
                OnPropertyChanged(nameof(SyncStatusText));
                break;

            case nameof(ISyncingService.SyncStatusMessage):
                OnPropertyChanged(nameof(SyncStatusText));
                break;

            case nameof(ISyncingService.LastSynced):
                OnPropertyChanged(nameof(LastSyncedFormatted));
                OnPropertyChanged(nameof(ShowLastUpdated));
                break;

            case nameof(ISyncingService.HasAutoSync):
                OnPropertyChanged(nameof(IsAutoSyncDisabled));
                break;
        }
    }

    private void OnSyncStarted(object? sender, EventArgs e)
    {
        // Could trigger UI feedback here, like showing a subtle animation
    }

    private void OnSyncCompleted(object? sender, EventArgs e)
    {
        // Could trigger success feedback here, like a brief toast
    }

    private void OnSyncFailed(object? sender, SyncFailedEventArgs e)
    {
        // Could show error notification here
        // Example: await DisplayAlertAsync("Sync Failed", e.ErrorMessage, "OK");
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _updateTimer.Dispose();
        _syncingService.PropertyChanged -= OnSyncingServicePropertyChanged;
        _syncingService.SyncStarted -= OnSyncStarted;
        _syncingService.SyncCompleted -= OnSyncCompleted;
        _syncingService.SyncFailed -= OnSyncFailed;
    }

    #endregion
}
