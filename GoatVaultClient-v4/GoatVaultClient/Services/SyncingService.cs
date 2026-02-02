using System.Diagnostics;
using System.Timers;
using GoatVaultClient.Services;
using GoatVaultInfrastructure.Services.Vault;
using Timer = System.Timers.Timer;

namespace GoatVaultInfrastructure.Services;

/// <summary>
/// Service responsible for managing vault synchronization with both manual and automatic modes
/// </summary>
public class SyncingService : IDisposable
{
    #region Fields and Properties

    private readonly VaultService _vaultService;
    private readonly VaultSessionService _vaultSessionService;
    private readonly ConnectivityService _connectivityService;
    private readonly Timer _autoSyncTimer;
    private bool _isDisposed;
    private bool _hasUnsavedChanges;
    private DateTime _lastSyncTime;
    private bool _isSyncing;

    /// <summary>
    /// Indicates whether there are unsaved changes in the vault
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (_hasUnsavedChanges != value)
            {
                _hasUnsavedChanges = value;
                UnsavedChangesChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Indicates whether a sync operation is currently in progress
    /// </summary>
    public bool IsSyncing
    {
        get => _isSyncing;
        private set
        {
            if (_isSyncing != value)
            {
                _isSyncing = value;
                SyncingStateChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Gets the last successful sync timestamp
    /// </summary>
    public DateTime LastSyncTime => _lastSyncTime;

    /// <summary>
    /// Gets or sets whether automatic syncing is enabled
    /// </summary>
    public bool AutoSyncEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the auto-sync interval in minutes (default: 10)
    /// </summary>
    public int AutoSyncIntervalMinutes { get; set; } = 10;

    #endregion

    #region Events

    /// <summary>
    /// Fired when a sync operation completes (successfully or with error)
    /// </summary>
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    /// <summary>
    /// Fired when the unsaved changes state changes
    /// </summary>
    public event EventHandler<bool>? UnsavedChangesChanged;

    /// <summary>
    /// Fired when the syncing state changes
    /// </summary>
    public event EventHandler<bool>? SyncingStateChanged;

    #endregion

    #region Constructor

    public SyncingService(
        VaultService vaultService,
        VaultSessionService vaultSessionService,
        ConnectivityService connectivityService)
    {
        _vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        _vaultSessionService = vaultSessionService ?? throw new ArgumentNullException(nameof(vaultSessionService));
        _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));

        // Initialize auto-sync timer
        _autoSyncTimer = new Timer();
        _autoSyncTimer.Elapsed += OnAutoSyncTimerElapsed;

        // Subscribe to connectivity changes
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;

        _lastSyncTime = DateTime.MinValue;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts the automatic sync timer
    /// </summary>
    public void StartAutoSync()
    {
        if (!AutoSyncEnabled)
        {
            Debug.WriteLine("Auto-sync is disabled");
            return;
        }

        _autoSyncTimer.Interval = TimeSpan.FromMinutes(AutoSyncIntervalMinutes).TotalMilliseconds;
        _autoSyncTimer.Start();
        Debug.WriteLine($"Auto-sync started with interval: {AutoSyncIntervalMinutes} minutes");
    }

    /// <summary>
    /// Stops the automatic sync timer
    /// </summary>
    public void StopAutoSync()
    {
        _autoSyncTimer.Stop();
        Debug.WriteLine("Auto-sync stopped");
    }

    /// <summary>
    /// Manually triggers a sync operation
    /// </summary>
    /// <returns>Result of the sync operation</returns>
    public async Task<SyncResult> SyncNowAsync()
    {
        return await PerformSyncAsync(isManuallTriggered: true);
    }

    /// <summary>
    /// Marks the vault as having unsaved changes
    /// Call this method whenever the vault data is modified
    /// </summary>
    public void MarkAsChanged()
    {
        HasUnsavedChanges = true;
        Debug.WriteLine("Vault marked as changed");
    }

    /// <summary>
    /// Forces a save without checking for changes
    /// </summary>
    public async Task<SyncResult> ForceSyncAsync()
    {
        var previousState = HasUnsavedChanges;
        HasUnsavedChanges = true;
        var result = await PerformSyncAsync(isManuallTriggered: true);

        if (!result.Success)
        {
            HasUnsavedChanges = previousState;
        }

        return result;
    }

    #endregion

    #region Private Methods

    private async Task<SyncResult> PerformSyncAsync(bool isManuallTriggered)
    {
        // Prevent concurrent sync operations
        if (IsSyncing)
        {
            Debug.WriteLine("Sync already in progress, skipping");
            return new SyncResult
            {
                Success = false,
                Message = "A sync operation is already in progress",
                SyncType = isManuallTriggered ? SyncType.Manual : SyncType.Automatic
            };
        }

        IsSyncing = true;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check if user is logged in
            if (_vaultSessionService.CurrentUser == null)
            {
                return CreateResult(false, "No user is currently logged in", isManuallTriggered);
            }

            // Check if vault is unlocked
            if (_vaultSessionService.DecryptedVault == null)
            {
                return CreateResult(false, "Vault is locked", isManuallTriggered);
            }

            // Check if master password is available
            if (string.IsNullOrEmpty(_vaultSessionService.MasterPassword))
            {
                return CreateResult(false, "Master password not available", isManuallTriggered);
            }

            // For automatic sync, only proceed if there are unsaved changes
            if (!isManuallTriggered && !HasUnsavedChanges)
            {
                Debug.WriteLine("Auto-sync skipped: no unsaved changes");
                return CreateResult(true, "No changes to sync", isManuallTriggered);
            }

            // Check connectivity
            var isConnected = await _connectivityService.CheckConnectivityAsync();
            if (!isConnected)
            {
                return CreateResult(false, "No internet connection available", isManuallTriggered);
            }

            // Perform the actual sync
            await _vaultService.SyncAndCloseAsync(
                _vaultSessionService.CurrentUser,
                _vaultSessionService.MasterPassword,
                _vaultSessionService.DecryptedVault
            );

            // After successful sync, vault is automatically locked by SyncAndCloseAsync
            // We need to unlock it again for continued use
            // Note: In a production app, you might want to handle this differently

            stopwatch.Stop();
            _lastSyncTime = DateTime.UtcNow;
            HasUnsavedChanges = false;

            var successMessage = isManuallTriggered
                ? $"Vault synced successfully in {stopwatch.ElapsedMilliseconds}ms"
                : $"Auto-sync completed in {stopwatch.ElapsedMilliseconds}ms";

            Debug.WriteLine(successMessage);
            return CreateResult(true, successMessage, isManuallTriggered, stopwatch.ElapsedMilliseconds);
        }
        catch (InvalidOperationException ex)
        {
            // This typically happens when decryption fails
            Debug.WriteLine($"Sync failed - Invalid operation: {ex.Message}");
            return CreateResult(false, $"Sync failed: {ex.Message}", isManuallTriggered);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sync failed with error: {ex.Message}");
            return CreateResult(false, $"Sync failed: {ex.Message}", isManuallTriggered);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private SyncResult CreateResult(bool success, string message, bool isManual, long elapsedMs = 0)
    {
        var result = new SyncResult
        {
            Success = success,
            Message = message,
            SyncType = isManual ? SyncType.Manual : SyncType.Automatic,
            SyncTime = DateTime.UtcNow,
            ElapsedMilliseconds = elapsedMs
        };

        // Fire the completion event
        SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(result));

        return result;
    }

    private async void OnAutoSyncTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Debug.WriteLine("Auto-sync timer elapsed");
        await PerformSyncAsync(isManuallTriggered: false);
    }

    private async void OnConnectivityChanged(object? sender, bool isConnected)
    {
        Debug.WriteLine($"Connectivity changed: {isConnected}");

        // If we regain connectivity and have unsaved changes, trigger a sync
        if (isConnected && HasUnsavedChanges && AutoSyncEnabled)
        {
            Debug.WriteLine("Connectivity restored, attempting to sync unsaved changes");
            await Task.Delay(1000); // Brief delay to ensure connection is stable
            await PerformSyncAsync(isManuallTriggered: false);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
            return;

        StopAutoSync();
        _autoSyncTimer?.Dispose();
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Represents the result of a sync operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SyncType SyncType { get; set; }
    public DateTime SyncTime { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

/// <summary>
/// Type of sync operation
/// </summary>
public enum SyncType
{
    Manual,
    Automatic
}

/// <summary>
/// Event args for sync completion
/// </summary>
public class SyncCompletedEventArgs : EventArgs
{
    public SyncResult Result { get; }

    public SyncCompletedEventArgs(SyncResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}

#endregion