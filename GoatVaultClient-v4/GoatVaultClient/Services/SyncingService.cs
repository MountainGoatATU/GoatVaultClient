using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GoatVaultClient.Services
{
    public interface ISyncingService : INotifyPropertyChanged
    {
        // Methods
        Task Sync();
        Task Save();
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
        event EventHandler SyncStarted;
        event EventHandler SyncCompleted;
        event EventHandler<SyncFailedEventArgs> SyncFailed;
    }
    public class SyncingService : ISyncingService
    {
        private readonly IConfiguration _configuration;
        private readonly VaultSessionService _vaultSessionService;
        private readonly VaultService _vaultService;
        private readonly HttpService _httpService;
        private readonly GoatVaultDb _db;
        private readonly ILogger<SyncingService> _logger;

        private System.Threading.Timer _periodicSyncTimer;
        private bool _isSyncing;
        private SyncStatus _syncStatus = SyncStatus.Unsynced;
        private DateTime _lastSynced;
        private DateTime _lastSaved;
        private string _syncStatusMessage = "Not synced";

        public SyncingService(
             IConfiguration configuration,
             VaultSessionService vaultSessionService,
             VaultService vaultService,
             HttpService httpService,
             GoatVaultDb db,
             ILogger<SyncingService> logger = null)
        {
            _configuration = configuration;
            _vaultSessionService = vaultSessionService;
            _vaultService = vaultService;
            _httpService = httpService;
            _db = db;
            _logger = logger;
        }

        #region Properties

        public bool MarkedAsChanged { get; private set; }

        private bool _hasAutoSave = true;
        public bool HasAutoSave
        {
            get => _hasAutoSave;
            set
            {
                if (_hasAutoSave != value)
                {
                    _hasAutoSave = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _hasAutoSync = true;
        public bool HasAutoSync
        {
            get => _hasAutoSync;
            set
            {
                if (_hasAutoSync != value)
                {
                    _hasAutoSync = value;
                    OnPropertyChanged();

                    // Update periodic sync based on auto-sync setting
                    if (!value)
                    {
                        StopPeriodicSync();
                    }
                }
            }
        }
        public DateTime LastSynced
        {
            get => _lastSynced;
            private set
            {
                if (_lastSynced != value)
                {
                    _lastSynced = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LastSyncedFormatted));
                }
            }
        }
        public DateTime LastSaved
        {
            get => _lastSaved;
            private set
            {
                if (_lastSaved != value)
                {
                    _lastSaved = value;
                    OnPropertyChanged();
                }
            }
        }
        public SyncStatus SyncStatus
        {
            get => _syncStatus;
            private set
            {
                if (_syncStatus != value)
                {
                    _syncStatus = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsSyncing
        {
            get => _isSyncing;
            private set
            {
                if (_isSyncing != value)
                {
                    _isSyncing = value;
                    OnPropertyChanged();
                }
            }
        }
        public string SyncStatusMessage
        {
            get => _syncStatusMessage;
            private set
            {
                if (_syncStatusMessage != value)
                {
                    _syncStatusMessage = value;
                    OnPropertyChanged();
                }
            }
        }
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
                else if (timeSince.TotalMinutes < 60)
                    return $"{(int)timeSince.TotalMinutes}m ago";
                else if (timeSince.TotalHours < 24)
                    return $"{(int)timeSince.TotalHours}h ago";
                else
                    return $"{(int)timeSince.TotalDays}d ago";
            }
        }
        #endregion
        #region Events

        public event EventHandler SyncStarted;
        public event EventHandler SyncCompleted;
        public event EventHandler<SyncFailedEventArgs> SyncFailed;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Starts periodic background syncing at specified interval
        /// </summary>
        /// <param name="interval">Time interval between sync operations</param>
        public void StartPeriodicSync(TimeSpan interval)
        {
            StopPeriodicSync(); // Stop any existing timer

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
            // Prevent concurrent sync operations
            if (IsSyncing)
            {
                _logger?.LogWarning("Sync already in progress");
                return;
            }

            // Get server URL from configuration
            var url = _configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

            // Ensure user is logged in
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

                // Retrieve local stored vault
                var localCopy = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);

                // Pull the server stored vault
                var userResponse = await _httpService.GetAsync<UserResponse>(
                $"{url}v1/users/{_vaultSessionService.CurrentUser.Id}"
                );

                // Validating server response
                if (userResponse == null || localCopy == null)
                {
                    throw new InvalidOperationException("Failed to retrieve user data to compare");
                }

                // Compare timestamps
                var compareResult = DateTime.Compare(localCopy.UpdatedAt, userResponse.UpdatedAt);

                // If timestamps are equal, we are synced. If local is newer, prompt user to update server. If server is newer, prompt user to update local.
                if (compareResult == 0) // Synced
                {
                    _logger?.LogInformation("Data already in sync");
                    SyncStatus = SyncStatus.Synced;
                    SyncStatusMessage = "Synced";
                    LastSynced = DateTime.UtcNow;
                    SyncCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }
                else if (compareResult > 0) // Local is newer
                {
                    _logger?.LogInformation("Local data is newer, updating server");
                    await HandleLocalNewer(localCopy, url);
                }
                else // Server is newer
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
            // Validation
            if (_vaultSessionService.CurrentUser == null || _vaultSessionService.DecryptedVault == null)
            {
                _logger?.LogWarning("Cannot save: No user logged in");
                return;
            }

            try
            {
                _logger?.LogInformation("Saving vault locally");

                // Update the decrypted vault in session
                VaultData updatedVault = new VaultData
                {
                    Categories = _vaultSessionService.DecryptedVault.Categories,
                    Entries = _vaultSessionService.DecryptedVault.Entries

                };
                // Create encrypted vault
                var encryptedVault = _vaultService.EncryptVault(_vaultSessionService.MasterPassword, updatedVault);

                // Retrieve the user from the database
                var localUser = await _vaultService.LoadUserFromLocalAsync(_vaultSessionService.CurrentUser.Id);

                // Update the local user
                localUser.Email = _vaultSessionService.CurrentUser.Email;
                localUser.Vault = encryptedVault;
                localUser.UpdatedAt = DateTime.UtcNow;

                // Save user in local db
                await _vaultService.UpdateUserInLocalAsync(localUser);

                // Update last saved property
                LastSaved = DateTime.UtcNow;
                MarkedAsChanged = true;

                // Update sync status since local is now newer
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

        /// <summary>
        /// Callback for periodic sync timer
        /// </summary>
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

        /// <summary>
        /// Handles case where local data is newer than server
        /// </summary>
        private async Task HandleLocalNewer(DbModel localCopy, string url)
        {
            // Local vault decryption
            var decryptedLocalVault = _vaultService.DecryptVault(localCopy.Vault, _vaultSessionService.MasterPassword);

            // Update local session with local data
            _vaultSessionService.CurrentUser.Id = localCopy.Id;
            _vaultSessionService.CurrentUser.Email = localCopy.Email;
            _vaultSessionService.CurrentUser.MfaEnabled = localCopy.MfaEnabled;
            _vaultSessionService.CurrentUser.MfaSecret = localCopy.MfaSecret;
            _vaultSessionService.CurrentUser.UpdatedAt = localCopy.UpdatedAt;
            _vaultSessionService.CurrentUser.CreatedAt = localCopy.CreatedAt;
            _vaultSessionService.CurrentUser.Vault = localCopy.Vault;
            _vaultSessionService.DecryptedVault = decryptedLocalVault;

            // Update server with local data
            await UpdateFromLocalToServer(url);
        }
        /// <summary>
        /// Handles case where server data is newer than local
        /// </summary>
        private async Task HandleServerNewer(DbModel localCopy, UserResponse userResponse)
        {
            // Decrypt server vault
            var decryptedServerVault = _vaultService.DecryptVault(userResponse.Vault, _vaultSessionService.MasterPassword);

            // Update session with server data
            _vaultSessionService.CurrentUser.Id = userResponse.Id;
            _vaultSessionService.CurrentUser.Email = userResponse.Email;
            _vaultSessionService.CurrentUser.MfaEnabled = userResponse.MfaEnabled;
            _vaultSessionService.CurrentUser.MfaSecret = userResponse.MfaSecret;
            _vaultSessionService.CurrentUser.UpdatedAt = userResponse.UpdatedAt;
            _vaultSessionService.CurrentUser.CreatedAt = userResponse.CreatedAt;
            _vaultSessionService.CurrentUser.Vault = userResponse.Vault;
            _vaultSessionService.DecryptedVault = decryptedServerVault;

            // Update local storage with server data
            await UpdateFromServerToLocal(localCopy, userResponse);
        }
        private async Task UpdateFromLocalToServer(string url)
        {
            // Encrypt local vault
            var encryptedVault = _vaultService.EncryptVault(_vaultSessionService.MasterPassword, _vaultSessionService.DecryptedVault);

            // Create update request
            var updateUserRequest = new UserRequest
            {
                Email = _vaultSessionService.CurrentUser.Email,
                MfaEnabled = _vaultSessionService.CurrentUser.MfaEnabled,
                MfaSecret = _vaultSessionService.CurrentUser.MfaSecret,
                Vault = encryptedVault,
            };

            // Send update request to server
            var updateResult = await _httpService.PatchAsync<UserResponse>(
                $"{url}v1/users/{_vaultSessionService.CurrentUser.Id}",
                updateUserRequest
            );
        }
        private async Task UpdateFromServerToLocal(DbModel localData, UserResponse serverData)
        {
            // Update local copy with server data
            localData.Email = serverData.Email;
            localData.Vault = serverData.Vault;
            localData.UpdatedAt = serverData.UpdatedAt;

            // Wait for database update to complete
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
    /// <summary>
    /// Event args for sync failure events
    /// </summary>
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
