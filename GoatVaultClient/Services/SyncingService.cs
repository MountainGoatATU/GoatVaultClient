using CommunityToolkit.Mvvm.ComponentModel;
using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Email = GoatVaultCore.Models.Objects.Email;

namespace GoatVaultClient.Services;

public class SyncingService(
    ISessionContext session,
    IServiceProvider services,
    IServerAuthService serverAuth,
    IVaultCrypto vaultCrypto,
    ILogger<SyncingService>? logger = null)
    : ObservableObject, ISyncingService
{
    private Timer? _periodicSyncTimer;

    public bool IsSyncing
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public SyncStatus SyncStatus
    {
        get;
        private set => SetProperty(ref field, value);
    } = SyncStatus.Unsynced;

    public DateTime LastSynced
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(LastSyncedFormatted));
        }
    }

    public DateTime LastSaved
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string SyncStatusMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Not synced";

    public bool HasAutoSave
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool HasAutoSync
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && !value)
                StopPeriodicSync();
        }
    } = true;

    #region Properties

    public bool MarkedAsChanged { get; private set; }

    // Human-readable last synced time
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
    public event EventHandler? AuthenticationRequired;

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
                    logger?.LogError(e, "Error awaiting periodic sync callback");
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

            using var scope = services.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // 1. Get server version
            var serverUser = await serverAuth.GetUserAsync(session.UserId.Value);

            // 2. Get local version
            var localUser = await userRepository.GetByIdAsync(session.UserId.Value);

            if (localUser == null)
            {
                // Should not happen if session is active
                logger?.LogWarning("Local user not found, pulling from server.");
                await Task.Run(() => HandleServerNewer(null, serverUser, userRepository));
            }
            else
            {
                // Use a tolerance for comparison (e.g., 1 second) to handle precision differences
                var timeDiff = (localUser.UpdatedAtUtc - serverUser.UpdatedAtUtc).TotalSeconds;

                // Log the timestamps for debugging
                logger?.LogInformation("Sync Check - Local: {LocalTime}, Server: {ServerTime}, Diff: {Diff}s",
                    localUser.UpdatedAtUtc.ToString("O"),
                    serverUser.UpdatedAtUtc.ToString("O"),
                    timeDiff);

                if (Math.Abs(timeDiff) < 1.0)
                {
                    logger?.LogInformation("Data already in sync (within tolerance)");
                }
                else if (timeDiff > 0)
                {
                    logger?.LogInformation("Local data is newer ({Local} > {Server}), pushing to server", localUser.UpdatedAtUtc, serverUser.UpdatedAtUtc);
                    await Task.Run(() => HandleLocalNewer(localUser, userRepository));
                }
                else
                {
                    logger?.LogInformation("Server data is newer ({Server} > {Local}), pulling from server", serverUser.UpdatedAtUtc, localUser.UpdatedAtUtc);
                    await Task.Run(() => HandleServerNewer(localUser, serverUser, userRepository));
                }
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
        if (session.UserId == null || session.Vault == null)
        {
            logger?.LogWarning("Cannot save: No user logged in");
            return;
        }

        try
        {
            logger?.LogInformation("Saving vault locally");

            // Trigger the use case that encrypts and saves to local DB
            using var scope = services.CreateScope();
            var saveVault = scope.ServiceProvider.GetRequiredService<SaveVaultUseCase>();
            await saveVault.ExecuteAsync();

            LastSaved = DateTime.UtcNow;
            MarkedAsChanged = true;

            SyncStatus = SyncStatus.Unsynced;
            SyncStatusMessage = "Changes pending";

            logger?.LogInformation("Vault saved successfully");

            // Trigger sync to push changes if auto-sync is on
            if (HasAutoSync)
            {
                _ = Task.Run(Sync);
            }
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
            await Task.Run(() => Sync());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Periodic sync failed");
        }
    }

    private async Task HandleLocalNewer(User localUser, IUserRepository userRepository)
    {
        var request = new UpdateVaultRequest { Vault = localUser.Vault };

        var updatedServerUser = await serverAuth.UpdateUserAsync(localUser.Id, request);

        // Update local timestamp
        localUser.UpdatedAtUtc = updatedServerUser.UpdatedAtUtc;
        await userRepository.SaveAsync(localUser);
    }

    private async Task HandleServerNewer(User? localUser, UserResponse serverUser, IUserRepository userRepository)
    {
        // 1. Decrypt new vault
        var masterKey = session.GetMasterKey();

        VaultDecrypted decryptedVault;
        try
        {
            decryptedVault = vaultCrypto.Decrypt(serverUser.Vault, masterKey);
        }
        catch (CryptographicException)
        {
            logger?.LogWarning("Failed to decrypt server vault. The password might have changed on another device.");
            AuthenticationRequired?.Invoke(this, EventArgs.Empty);
            throw;
        }

        // 2. Update Session
        session.UpdateVault(decryptedVault);

        // Re-fetch the user to ensure we are updating the tracked entity if it exists
        var existingUser = await userRepository.GetByIdAsync(Guid.Parse(serverUser.Id));

        if (existingUser != null)
            localUser = existingUser;

        var mfaSecret = serverUser.MfaSecret is not null
            ? Convert.FromBase64String(serverUser.MfaSecret)
            : [];

        // 3. Update Local DB
        if (localUser == null)
        {
            localUser = new User
            {
                Id = Guid.Parse(serverUser.Id),
                AuthSalt = Convert.FromBase64String(serverUser.AuthSalt),
                AuthVerifier = Convert.FromBase64String(serverUser.AuthVerifier),
                CreatedAtUtc = serverUser.CreatedAtUtc,
                MfaEnabled = serverUser.MfaEnabled,
                MfaSecret = mfaSecret,
                ShamirEnabled = serverUser.ShamirEnabled,
                Argon2Parameters = serverUser.Argon2Parameters ?? GoatVaultCore.Models.Objects.Argon2Parameters.Default,
                VaultSalt = Convert.FromBase64String(serverUser.VaultSalt),
                UpdatedAtUtc = serverUser.UpdatedAtUtc
            };
        }
        else
        {
            // Preserve existing AuthVerifier/MfaSecret if updating existing user
            localUser.AuthSalt = Convert.FromBase64String(serverUser.AuthSalt);
            localUser.Argon2Parameters = serverUser.Argon2Parameters ?? localUser.Argon2Parameters;
        }

        localUser.Email = new Email(serverUser.Email);
        localUser.Vault = serverUser.Vault;
        localUser.VaultSalt = Convert.FromBase64String(serverUser.VaultSalt);
        localUser.MfaEnabled = serverUser.MfaEnabled;
        localUser.MfaSecret = mfaSecret;
        localUser.ShamirEnabled = serverUser.ShamirEnabled;
        localUser.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await userRepository.SaveAsync(localUser);
    }

    #endregion
}
