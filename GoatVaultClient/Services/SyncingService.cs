using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace GoatVaultClient.Services
{
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
    }

    public class SyncingService : ObservableObject, ISyncingService
    {
        private readonly IConfiguration _configuration;
        private readonly VaultSessionService _vaultSessionService;
        private readonly VaultService _vaultService;
        private readonly HttpService _httpService;
        private readonly ILogger<SyncingService>? _logger;

        private System.Threading.Timer? _periodicSyncTimer;

        private bool _isSyncing;
        public bool IsSyncing
        {
            get => _isSyncing;
            private set => SetProperty(ref _isSyncing, value);
        }

        private SyncStatus _syncStatus = SyncStatus.Unsynced;
        public SyncStatus SyncStatus
        {
            get => _syncStatus;
            private set => SetProperty(ref _syncStatus, value);
        }

        private DateTime _lastSynced;
        public DateTime LastSynced
        {
            get => _lastSynced;
            private set
            {
                if (SetProperty(ref _lastSynced, value))
                    OnPropertyChanged(nameof(LastSyncedFormatted));
            }
        }

        private DateTime _lastSaved;
        public DateTime LastSaved
        {
            get => _lastSaved;
            private set => SetProperty(ref _lastSaved, value);
        }

        private string _syncStatusMessage = "Not synced";
        public string SyncStatusMessage
        {
            get => _syncStatusMessage;
            private set => SetProperty(ref _syncStatusMessage, value);
        }

        private bool _hasAutoSave = true;
        public bool HasAutoSave
        {
            get => _hasAutoSave;
            set => SetProperty(ref _hasAutoSave, value);
        }

        private bool _hasAutoSync = true;
        public bool HasAutoSync
        {
            get => _hasAutoSync;
            set
            {
                if (SetProperty(ref _hasAutoSync, value) && !value)
                    StopPeriodicSync();
            }
        }

        public SyncingService(
             IConfiguration configuration,
             VaultSessionService vaultSessionService,
             VaultService vaultService,
             HttpService httpService,
             ILogger<SyncingService>? logger = null)
        {
            _configuration = configuration;
            _vaultSessionService = vaultSessionService;
            _vaultService = vaultService;
            _httpService = httpService;
            _logger = logger;
        }

        #region Properties

        public bool MarkedAsChanged { get; private set; }

        /// <summary>
        /// Human-readable last synced time
        /// </summary>
        public string LastSyncedFormatted
        {
            get
            {
                if (LastSynced == default)
                    return "Never";

                var timeSince = DateTime.UtcNow - LastSynced;

                if (timeSince.TotalMinutes < 1)
                    return "Just now";
                if (timeSince.TotalMinutes < 60)
                    return $"{(int)timeSince.TotalMinutes}m ago";
                if (timeSince.TotalHours < 24)
                    return $"{(int)timeSince.TotalHours}h ago";
                return $"{(int)timeSince.TotalDays}d ago";
            }
        }

        #endregion
        #region Events

        public event EventHandler? SyncStarted;
        public event EventHandler? SyncCompleted;
        public event EventHandler<SyncFailedEventArgs>? SyncFailed;

        #endregion
        #region Public Methods

        /// <summary>
        /// Saves if auto-save is enabled. Convenience method to reduce repeated checks.
        /// </summary>
        public async Task AutoSaveIfEnabled()
        {
            if (HasAutoSave)
                await Save();
        }

        /// <summary>
        /// Starts periodic background syncing at specified interval
        /// </summary>
        public void StartPeriodicSync(TimeSpan interval)
        {
            StopPeriodicSync();

            if (!HasAutoSync)
            {
                _logger?.LogInformation("Auto-sync is disabled. Periodic sync will not start.");
                return;
            }

            _periodicSyncTimer = new System.Threading.Timer(
                async _ => await PeriodicSyncCallback(),
                null,
                interval,
                interval);

            _logger?.LogInformation($"Periodic sync started with interval: {interval}");
        }

        /// <summary>
        /// Stops periodic background syncing
        /// </summary>
        public void StopPeriodicSync()
        {
            _periodicSyncTimer?.Dispose();
            _periodicSyncTimer = null;
            _logger?.LogInformation("Periodic sync stopped");
        }

        /// <summary>
        /// Main sync method - synchronizes local and server data
        /// </summary>
        public async Task Sync()
        {
            if (IsSyncing)
            {
                _logger?.LogWarning("Sync already in progress");
                return;
            }

            var url = _configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

            if (_vaultSessionService.CurrentUser == null && _vaultSessionService.DecryptedVault == null)
            {
                _logger?.LogWarning("No user is currently logged in");
                return;
            }

            try
            {
                IsSyncing = true;
                SyncStatus = SyncStatus.Syncing;
                SyncStatusMessage = "Syncing...";
                SyncStarted?.Invoke(this, EventArgs.Empty);

                _logger?.LogInformation("Starting sync operation");

                var localCopy = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);

                var userResponse = await _httpService.GetAsync<UserResponse>(
                    $"{url}v1/users/{_vaultSessionService.CurrentUser.Id}"
                );

                if (userResponse == null || localCopy == null)
                    throw new InvalidOperationException("Failed to retrieve user data to compare");

                var compareResult = DateTime.Compare(localCopy.UpdatedAt, userResponse.UpdatedAt);

                if (compareResult == 0)
                {
                    _logger?.LogInformation("Data already in sync");
                    SyncStatus = SyncStatus.Synced;
                    SyncStatusMessage = "Synced";
                    LastSynced = DateTime.UtcNow;
                    SyncCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (compareResult > 0)
                {
                    _logger?.LogInformation("Local data is newer, updating server");
                    await HandleLocalNewer(localCopy, url);
                }
                else
                {
                    _logger?.LogInformation("Server data is newer, updating local");
                    await HandleServerNewer(localCopy, userResponse);
                }

                SyncStatus = SyncStatus.Synced;
                SyncStatusMessage = "Synced";
                LastSynced = DateTime.UtcNow;
                SyncCompleted?.Invoke(this, EventArgs.Empty);

                _logger?.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Sync failed");
                SyncStatus = SyncStatus.Failed;
                SyncStatusMessage = "Sync failed";
                SyncFailed?.Invoke(this, new SyncFailedEventArgs(ex));
            }
            finally
            {
                IsSyncing = false;
            }
        }

        public async Task Save()
        {
            if (_vaultSessionService.CurrentUser == null || _vaultSessionService.DecryptedVault == null)
            {
                _logger?.LogWarning("Cannot save: No user logged in");
                return;
            }

            try
            {
                _logger?.LogInformation("Saving vault locally");

                var updatedVault = new VaultData
                {
                    Categories = _vaultSessionService.DecryptedVault.Categories,
                    Entries = _vaultSessionService.DecryptedVault.Entries
                };

                var encryptedVault = _vaultService.EncryptVault(_vaultSessionService.MasterPassword, updatedVault);

                var localUser = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);

                localUser.Email = _vaultSessionService.CurrentUser.Email;
                localUser.Vault = encryptedVault;
                localUser.UpdatedAt = DateTime.UtcNow;

                await _vaultService.UpdateUserInLocalAsync(localUser);

                LastSaved = DateTime.UtcNow;
                MarkedAsChanged = true;

                SyncStatus = SyncStatus.Unsynced;
                SyncStatusMessage = "Changes pending";

                _logger?.LogInformation("Vault saved successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save vault");
            }
        }

        #endregion
        #region Private Methods

        private async Task PeriodicSyncCallback()
        {
            if (!HasAutoSync)
                return;

            try
            {
                await Sync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Periodic sync failed");
            }
        }

        private async Task HandleLocalNewer(DbModel localCopy, string url)
        {
            var decryptedLocalVault = _vaultService.DecryptVault(localCopy.Vault, _vaultSessionService.MasterPassword);

            _vaultSessionService.CurrentUser.Id = localCopy.Id;
            _vaultSessionService.CurrentUser.Email = localCopy.Email;
            _vaultSessionService.CurrentUser.MfaEnabled = localCopy.MfaEnabled;
            _vaultSessionService.CurrentUser.MfaSecret = localCopy.MfaSecret;
            _vaultSessionService.CurrentUser.UpdatedAt = localCopy.UpdatedAt;
            _vaultSessionService.CurrentUser.CreatedAt = localCopy.CreatedAt;
            _vaultSessionService.CurrentUser.Vault = localCopy.Vault;
            _vaultSessionService.DecryptedVault = decryptedLocalVault;

            await UpdateFromLocalToServer(url);
        }

        private async Task HandleServerNewer(DbModel localCopy, UserResponse userResponse)
        {
            var decryptedServerVault = _vaultService.DecryptVault(userResponse.Vault, _vaultSessionService.MasterPassword);

            _vaultSessionService.CurrentUser.Id = userResponse.Id;
            _vaultSessionService.CurrentUser.Email = userResponse.Email;
            _vaultSessionService.CurrentUser.MfaEnabled = userResponse.MfaEnabled;
            _vaultSessionService.CurrentUser.MfaSecret = userResponse.MfaSecret;
            _vaultSessionService.CurrentUser.UpdatedAt = userResponse.UpdatedAt;
            _vaultSessionService.CurrentUser.CreatedAt = userResponse.CreatedAt;
            _vaultSessionService.CurrentUser.Vault = userResponse.Vault;
            _vaultSessionService.DecryptedVault = decryptedServerVault;

            await UpdateFromServerToLocal(localCopy, userResponse);
        }

        private async Task UpdateFromLocalToServer(string url)
        {
            var encryptedVault = _vaultService.EncryptVault(_vaultSessionService.MasterPassword, _vaultSessionService.DecryptedVault);

            var updateUserRequest = new UserRequest
            {
                Email = _vaultSessionService.CurrentUser.Email,
                MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled,
                MfaSecret = _vaultSessionService.CurrentUser.MfaSecret,
                Vault = encryptedVault,
            };

            await _httpService.PatchAsync<UserResponse>(
                $"{url}v1/users/{_vaultSessionService.CurrentUser.Id}",
                updateUserRequest
            );
        }

        private async Task UpdateFromServerToLocal(DbModel localData, UserResponse serverData)
        {
            localData.Email = serverData.Email;
            localData.Vault = serverData.Vault;
            localData.UpdatedAt = serverData.UpdatedAt;

            await _vaultService.UpdateUserInLocalAsync(localData);
        }

        #endregion
    }

    public enum SyncStatus
    {
        Synced,
        Unsynced,
        Syncing,
        Failed
    }

    public class SyncFailedEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string ErrorMessage => Exception?.Message ?? "Unknown error";

        public SyncFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
