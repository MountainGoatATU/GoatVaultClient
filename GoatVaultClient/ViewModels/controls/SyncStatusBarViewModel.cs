using GoatVaultClient.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace GoatVaultClient.ViewModels.controls
{
    /// <summary>
    /// ViewModel for the SyncStatusBar control
    /// Manages state and commands for the sync status display
    /// </summary>
    public class SyncStatusBarViewModel : INotifyPropertyChanged
    {
        private readonly ISyncingService _syncingService;
        private readonly Timer _updateTimer;

        public SyncStatusBarViewModel(ISyncingService syncingService)
        {
            _syncingService = syncingService;

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
                    throw; // TODO handle exception
                }
            }, CanExecuteSync);

            // Start a timer to update the "Last synced" time display every minute
            _updateTimer = new System.Threading.Timer(
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
            catch (Exception ex)
            {
                // Error is already handled in the service
                // Could show a toast notification here if desired
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if sync is currently in progress
        /// </summary>
        public bool IsSyncing => _syncingService.IsSyncing;

        /// <summary>
        /// Indicates if sync completed successfully
        /// </summary>
        public bool IsSynced => _syncingService.SyncStatus == SyncStatus.Synced;

        /// <summary>
        /// Indicates if sync failed
        /// </summary>
        public bool SyncFailed => _syncingService.SyncStatus == SyncStatus.Failed;

        /// <summary>
        /// Text to display for current sync status
        /// </summary>
        public string SyncStatusText => _syncingService.SyncStatusMessage;

        /// <summary>
        /// Formatted last synced time
        /// </summary>
        public string LastSyncedFormatted => _syncingService.LastSyncedFormatted;

        /// <summary>
        /// Controls visibility of sync button based on auto-sync setting
        /// </summary>
        public bool ShowSyncButton => true; // Always show, but can be controlled by settings

        /// <summary>
        /// Controls whether sync button is enabled
        /// </summary>
        public bool IsSyncButtonEnabled => !_syncingService.IsSyncing;

        /// <summary>
        /// Controls visibility of sync status indicator
        /// </summary>
        public bool ShowSyncStatus => true;

        /// <summary>
        /// Controls visibility of last updated label
        /// </summary>
        public bool ShowLastUpdated => _syncingService.LastSynced != default;

        /// <summary>
        /// Indicates if auto-sync is disabled
        /// </summary>
        public bool IsAutoSyncDisabled => !_syncingService.HasAutoSync;

        #endregion

        #region Event Handlers

        private void OnSyncingServicePropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void OnSyncStarted(object sender, EventArgs e)
        {
            // Could trigger UI feedback here, like showing a subtle animation
        }

        private void OnSyncCompleted(object sender, EventArgs e)
        {
            // Could trigger success feedback here, like a brief toast
        }

        private void OnSyncFailed(object sender, SyncFailedEventArgs e)
        {
            // Could show error notification here
            // Example: await DisplayAlert("Sync Failed", e.ErrorMessage, "OK");
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _updateTimer?.Dispose();
            _syncingService.PropertyChanged -= OnSyncingServicePropertyChanged;
            _syncingService.SyncStarted -= OnSyncStarted;
            _syncingService.SyncCompleted -= OnSyncCompleted;
            _syncingService.SyncFailed -= OnSyncFailed;
        }

        #endregion
    }
}
