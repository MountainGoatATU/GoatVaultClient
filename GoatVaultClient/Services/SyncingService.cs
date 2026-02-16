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

    public class SyncingService(
        IConfiguration configuration,
        VaultSessionService vaultSessionService,
        VaultService vaultService,
        HttpService httpService,
        ILogger<SyncingService>? logger = null)
        : ObservableObject, ISyncingService
    {
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

                return timeSince.TotalMinutes switch
                {
                    < 1 => "Just now",
                    < 60 => $"{(int)timeSince.TotalMinutes}m ago",
                    _ => timeSince.TotalHours < 24
                        ? $"{(int)timeSince.TotalHours}h ago"
                        : $"{(int)timeSince.TotalDays}d ago"
                };
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
                logger?.LogInformation("Auto-sync is disabled. Periodic sync will not start.");
                return;
            }

            _periodicSyncTimer = new Timer(
                async void (_) =>
                {
                    try
                    {
                        await PeriodicSyncCallback();
                    }
                    catch (Exception e)
                    {
                        throw; // TODO handle exception
                    }
                },
                null,
                interval,
                interval);

            logger?.LogInformation("Periodic sync started with interval: {interval}", interval);
        }

        /// <summary>
        /// Stops periodic background syncing
        /// </summary>
        public void StopPeriodicSync()
        {
            _periodicSyncTimer?.Dispose();
            _periodicSyncTimer = null;
            logger?.LogInformation("Periodic sync stopped");
        }

        /// <summary>
        /// Main sync method - synchronizes local and server data
        /// </summary>
        public async Task Sync()
        {
            if (IsSyncing)
            {
                logger?.LogWarning("Sync already in progress");
                return;
            }

            var url = configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

            if (vaultSessionService.CurrentUser == null || vaultSessionService.DecryptedVault == null)
            {
                logger?.LogWarning("No user is currently logged in");
                return;
            }

            try
            {
                IsSyncing = true;
                SyncStatus = SyncStatus.Syncing;
                SyncStatusMessage = "Syncing...";
                SyncStarted?.Invoke(this, EventArgs.Empty);

                logger?.LogInformation("Starting sync operation");

                var localCopy = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser?.Id ?? throw new NullReferenceException());

                var userResponse = await httpService.GetAsync<UserResponse>(
                    $"{url}v1/users/{vaultSessionService.CurrentUser.Id}"
                );

                if (userResponse == null)
                    throw new InvalidOperationException("Failed to load user profile");

                if (localCopy == null)
                {
                    logger?.LogInformation("Local copy missing - pulling server copy");

                    await vaultService.SaveUserToLocalAsync(new DbModel
                    {
                        Id = userResponse.Id,
                        Email = userResponse.Email,
                        Vault = userResponse.Vault,
                        AuthSalt = userResponse.AuthSalt,
                        UpdatedAt = userResponse.UpdatedAt,
                        CreatedAt = userResponse.CreatedAt,
                        MfaEnabled = userResponse.MfaEnabled,
                        MfaSecret = userResponse.MfaSecret
                    });

                    return;
                }

                var compareResult = DateTime.Compare(localCopy.UpdatedAt, userResponse.UpdatedAt);

                switch (compareResult)
                {
                    case 0:
                        logger?.LogInformation("Data already in sync");
                        SyncStatus = SyncStatus.Synced;
                        SyncStatusMessage = "Synced";
                        LastSynced = DateTime.UtcNow;
                        SyncCompleted?.Invoke(this, EventArgs.Empty);
                        return;
                    case > 0:
                        logger?.LogInformation("Local data is newer, updating server");
                        await HandleLocalNewer(localCopy, url);
                        break;
                    default:
                        logger?.LogInformation("Server data is newer, updating local");
                        await HandleServerNewer(localCopy, userResponse);
                        break;
                }

                SyncStatus = SyncStatus.Synced;
                SyncStatusMessage = "Synced";
                LastSynced = DateTime.UtcNow;
                SyncCompleted?.Invoke(this, EventArgs.Empty);

                logger?.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Sync failed");
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
            if (vaultSessionService.CurrentUser == null || vaultSessionService.DecryptedVault == null)
            {
                logger?.LogWarning("Cannot save: No user logged in");
                return;
            }

            try
            {
                logger?.LogInformation("Saving vault locally");

                var updatedVault = new DecryptedVault
                {
                    Categories = vaultSessionService.DecryptedVault.Categories,
                    Entries = vaultSessionService.DecryptedVault.Entries
                };

                var encryptedVault = vaultService.EncryptVault(vaultSessionService.MasterPassword, updatedVault);

                var localUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

                if (localUser == null)
                    throw new InvalidOperationException("Local user missing during save");

                localUser.Email = vaultSessionService.CurrentUser.Email;
                localUser.Vault = encryptedVault;
                localUser.UpdatedAt = DateTime.UtcNow;

                await vaultService.UpdateUserInLocalAsync(localUser);

                LastSaved = DateTime.UtcNow;
                MarkedAsChanged = true;

                SyncStatus = SyncStatus.Unsynced;
                SyncStatusMessage = "Changes pending";

                logger?.LogInformation("Vault saved successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to save vault");
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
                logger?.LogError(ex, "Periodic sync failed");
            }
        }

        private async Task HandleLocalNewer(DbModel localCopy, string url)
        {
            var decryptedLocalVault = vaultService.DecryptVault(localCopy.Vault, vaultSessionService.MasterPassword);

            vaultSessionService.CurrentUser.Id = localCopy.Id;
            vaultSessionService.CurrentUser.Email = localCopy.Email;
            vaultSessionService.CurrentUser.MfaEnabled = localCopy.MfaEnabled;
            vaultSessionService.CurrentUser.MfaSecret = localCopy.MfaSecret;
            vaultSessionService.CurrentUser.UpdatedAt = localCopy.UpdatedAt;
            vaultSessionService.CurrentUser.CreatedAt = localCopy.CreatedAt;
            vaultSessionService.CurrentUser.Vault = localCopy.Vault;
            vaultSessionService.DecryptedVault = decryptedLocalVault;

            await UpdateFromLocalToServer(url);
        }

        private async Task HandleServerNewer(DbModel localCopy, UserResponse userResponse)
        {
            var decryptedServerVault = vaultService.DecryptVault(userResponse.Vault, vaultSessionService.MasterPassword);

            vaultSessionService.CurrentUser.Id = userResponse.Id;
            vaultSessionService.CurrentUser.Email = userResponse.Email;
            vaultSessionService.CurrentUser.MfaEnabled = userResponse.MfaEnabled;
            vaultSessionService.CurrentUser.MfaSecret = userResponse.MfaSecret;
            vaultSessionService.CurrentUser.UpdatedAt = userResponse.UpdatedAt;
            vaultSessionService.CurrentUser.CreatedAt = userResponse.CreatedAt;
            vaultSessionService.CurrentUser.Vault = userResponse.Vault;
            vaultSessionService.DecryptedVault = decryptedServerVault;

            await UpdateFromServerToLocal(localCopy, userResponse);
        }

        private async Task UpdateFromLocalToServer(string url)
        {
            var encryptedVault = vaultService.EncryptVault(vaultSessionService.MasterPassword, vaultSessionService.DecryptedVault);

            var updateUserRequest = new UserRequest
            {
                Email = vaultSessionService.CurrentUser.Email,
                MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
                MfaSecret = vaultSessionService.CurrentUser.MfaSecret,
                Vault = encryptedVault,
            };

            await httpService.PatchAsync<UserResponse>(
                $"{url}v1/users/{vaultSessionService.CurrentUser.Id}",
                updateUserRequest
            );
        }

        private async Task UpdateFromServerToLocal(DbModel localData, UserResponse serverData)
        {
            localData.Email = serverData.Email;
            localData.Vault = serverData.Vault;
            localData.UpdatedAt = serverData.UpdatedAt;

            await vaultService.UpdateUserInLocalAsync(localData);
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

    public class SyncFailedEventArgs(Exception exception) : EventArgs
    {
        public Exception Exception { get; } = exception;
        public string ErrorMessage => Exception?.Message ?? "Unknown error";
    }
}
