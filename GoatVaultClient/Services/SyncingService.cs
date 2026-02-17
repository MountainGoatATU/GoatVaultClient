using GoatVaultApplication.VaultUseCases;
using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultInfrastructure.Services.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Email = GoatVaultCore.Models.Email;

namespace GoatVaultClient.Services;

public class SyncingService(
    IConfiguration configuration,
    ISessionContext session,
    IServiceProvider services,
    IHttpService http,
    IVaultCrypto vaultCrypto,
    ILogger<SyncingService>? logger = null)
    : ObservableObject, ISyncingService
{
    private Timer? _periodicSyncTimer;

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

    // Saves if auto-save is enabled. Convenience method to reduce repeated checks.
    public async Task AutoSaveIfEnabled()
    {
        if (HasAutoSave)
            await Save();
    }

    // Starts periodic background syncing at specified interval
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

    // Stops periodic background syncing
    public void StopPeriodicSync()
    {
        _periodicSyncTimer?.Dispose();
        _periodicSyncTimer = null;
        logger?.LogInformation("Periodic sync stopped");
    }

    // Main sync method - synchronizes local and server data
    public async Task Sync()
    {
        if (IsSyncing)
        {
            logger?.LogWarning("Sync already in progress");
            return;
        }

        var url = configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

        if (session.UserId == null || session.Vault == null)
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

            // TODO: Fix
            /*
            var localCopy = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser?.Id ?? throw new NullReferenceException());

            var userResponse = await http.GetAsync<UserResponse>(
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
                    UpdatedAtUtc = userResponse.UpdatedAtUtc,
                    CreatedAtUtc = userResponse.CreatedAtUtc,
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
            */

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
        if (session.UserId == null || session.Vault == null)
        {
            logger?.LogWarning("Cannot save: No user logged in");
            return;
        }

        try
        {
            logger?.LogInformation("Saving vault locally");

            var updatedVault = new VaultDecrypted
            {
                Categories = session.Vault.Categories,
                Entries = session.Vault.Entries
            };

            // var encryptedVault = vaultCrypto.Encrypt(updatedVault, session.GetMasterKey());

            using var scope = services.CreateScope();
            var saveVault = scope.ServiceProvider.GetRequiredService<SaveVaultUseCase>();
            await saveVault.ExecuteAsync();

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

    private async Task HandleLocalNewer(User user, string url)
    {
        var decryptedLocalVault = vaultCrypto.Decrypt(user.Vault, session.GetMasterKey());

        // TODO: Fix
        /*
        vaultSessionService.CurrentUser.Id = user.Id;
        vaultSessionService.CurrentUser.Email = user.Email;
        vaultSessionService.CurrentUser.MfaEnabled = user.MfaEnabled;
        vaultSessionService.CurrentUser.MfaSecret = user.MfaSecret;
        vaultSessionService.CurrentUser.UpdatedAtUtc = user.UpdatedAtUtc;
        vaultSessionService.CurrentUser.CreatedAtUtc = user.CreatedAtUtc;
        vaultSessionService.CurrentUser.Vault = user.Vault;
        vaultSessionService.DecryptedVault = decryptedLocalVault;
        */

        await UpdateFromLocalToServer(url);
    }

    private async Task HandleServerNewer(User user, UserResponse userOnServer)
    {
        var decryptedLocalVault = vaultCrypto.Decrypt(user.Vault, session.GetMasterKey());

        // TODO: Fix
        /*
        vaultSessionService.CurrentUser.Id = userOnServer.Id;
        vaultSessionService.CurrentUser.Email = userOnServer.Email;
        vaultSessionService.CurrentUser.MfaEnabled = userOnServer.MfaEnabled;
        vaultSessionService.CurrentUser.MfaSecret = userOnServer.MfaSecret;
        vaultSessionService.CurrentUser.UpdatedAt = userOnServer.UpdatedAt;
        vaultSessionService.CurrentUser.CreatedAt = userOnServer.CreatedAt;
        vaultSessionService.CurrentUser.Vault = userOnServer.Vault;
        vaultSessionService.DecryptedVault = decryptedServerVault;
        */

        await UpdateFromServerToLocal(user, userOnServer);
    }

    private async Task UpdateFromLocalToServer(string url)
    {
        if (session.Vault is null)
            return;

        // var encryptedVault = vaultCrypto.Encrypt(session.Vault, session.GetMasterKey());

        /*
        var updateUserRequest = new UserRequest
        {
            Email = vaultSessionService.CurrentUser.Email,
            MfaEnabled = vaultSessionService.CurrentUser.MfaEnabled,
            MfaSecret = vaultSessionService.CurrentUser.MfaSecret,
            Vault = encryptedVault,
        };

        await http.PatchAsync<UserResponse>(
            $"{url}v1/users/{vaultSessionService.CurrentUser.Id}",
            updateUserRequest
        );
        */
    }

    private async Task UpdateFromServerToLocal(User user, UserResponse userOnServer)
    {
        user.Email = new Email(userOnServer.Email);
        user.Vault = userOnServer.Vault;
        user.UpdatedAtUtc = userOnServer.UpdatedAtUtc;

        // TODO: Fix
        // await vaultService.UpdateUserInLocalAsync(user);
    }

    #endregion
}