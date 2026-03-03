namespace GoatVaultClient.Controls;

public partial class SyncStatusBar
{
    private bool _isRotating;

    public SyncStatusBar()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Start rotation animation when IsSyncing becomes true
        if (propertyName != nameof(IsSyncing))
            return;

        if (IsSyncing && !_isRotating)
        {
            _ = AnimateSyncIcon();
        }
    }

    /// <summary>
    /// Animates the sync icon with continuous rotation
    /// </summary>
    private async Task AnimateSyncIcon()
    {
        _isRotating = true;

        while (IsSyncing && _isRotating)
        {
            await SyncingIcon.RotateToAsync(360, 1000, Easing.Linear);
            SyncingIcon.Rotation = 0; // Reset rotation for continuous animation
        }

        _isRotating = false;
    }

    #region Bindable Properties

    /// <summary>
    /// Command to execute when sync button is clicked
    /// </summary>
    public static readonly BindableProperty SyncCommandProperty =
        BindableProperty.Create(
            nameof(SyncCommand),
            typeof(Command),
            typeof(SyncStatusBar),
            null);

    public Command SyncCommand
    {
        get => (Command)GetValue(SyncCommandProperty);
        set => SetValue(SyncCommandProperty, value);
    }

    /// <summary>
    /// Indicates if sync is currently in progress
    /// </summary>
    public static readonly BindableProperty IsSyncingProperty =
        BindableProperty.Create(
            nameof(IsSyncing),
            typeof(bool),
            typeof(SyncStatusBar),
            false);

    public bool IsSyncing
    {
        get => (bool)GetValue(IsSyncingProperty);
        set => SetValue(IsSyncingProperty, value);
    }

    /// <summary>
    /// Indicates if sync completed successfully
    /// </summary>
    public static readonly BindableProperty IsSyncedProperty =
        BindableProperty.Create(
            nameof(IsSynced),
            typeof(bool),
            typeof(SyncStatusBar),
            false);

    public bool IsSynced
    {
        get => (bool)GetValue(IsSyncedProperty);
        set => SetValue(IsSyncedProperty, value);
    }

    /// <summary>
    /// Indicates if sync failed
    /// </summary>
    public static readonly BindableProperty SyncFailedProperty =
        BindableProperty.Create(
            nameof(SyncFailed),
            typeof(bool),
            typeof(SyncStatusBar),
            false);

    public bool SyncFailed
    {
        get => (bool)GetValue(SyncFailedProperty);
        set => SetValue(SyncFailedProperty, value);
    }

    /// <summary>
    /// Text to display for sync status
    /// </summary>
    public static readonly BindableProperty SyncStatusTextProperty =
        BindableProperty.Create(
            nameof(SyncStatusText),
            typeof(string),
            typeof(SyncStatusBar),
            "Synced");

    public string SyncStatusText
    {
        get => (string)GetValue(SyncStatusTextProperty);
        set => SetValue(SyncStatusTextProperty, value);
    }

    /// <summary>
    /// Formatted last synced time string
    /// </summary>
    public static readonly BindableProperty LastSyncedFormattedProperty =
        BindableProperty.Create(
            nameof(LastSyncedFormatted),
            typeof(string),
            typeof(SyncStatusBar),
            "Never");

    public string LastSyncedFormatted
    {
        get => (string)GetValue(LastSyncedFormattedProperty);
        set => SetValue(LastSyncedFormattedProperty, value);
    }

    /// <summary>
    /// Controls visibility of the sync button
    /// </summary>
    public static readonly BindableProperty ShowSyncButtonProperty =
        BindableProperty.Create(
            nameof(ShowSyncButton),
            typeof(bool),
            typeof(SyncStatusBar),
            true);

    public bool ShowSyncButton
    {
        get => (bool)GetValue(ShowSyncButtonProperty);
        set => SetValue(ShowSyncButtonProperty, value);
    }

    /// <summary>
    /// Controls whether the sync button is enabled
    /// </summary>
    public static readonly BindableProperty IsSyncButtonEnabledProperty =
        BindableProperty.Create(
            nameof(IsSyncButtonEnabled),
            typeof(bool),
            typeof(SyncStatusBar),
            true);

    public bool IsSyncButtonEnabled
    {
        get => (bool)GetValue(IsSyncButtonEnabledProperty);
        set => SetValue(IsSyncButtonEnabledProperty, value);
    }

    /// <summary>
    /// Controls visibility of sync status indicator
    /// </summary>
    public static readonly BindableProperty ShowSyncStatusProperty =
        BindableProperty.Create(
            nameof(ShowSyncStatus),
            typeof(bool),
            typeof(SyncStatusBar),
            true);

    public bool ShowSyncStatus
    {
        get => (bool)GetValue(ShowSyncStatusProperty);
        set => SetValue(ShowSyncStatusProperty, value);
    }

    /// <summary>
    /// Controls visibility of last updated label
    /// </summary>
    public static readonly BindableProperty ShowLastUpdatedProperty =
        BindableProperty.Create(
            nameof(ShowLastUpdated),
            typeof(bool),
            typeof(SyncStatusBar),
            true);

    public bool ShowLastUpdated
    {
        get => (bool)GetValue(ShowLastUpdatedProperty);
        set => SetValue(ShowLastUpdatedProperty, value);
    }

    /// <summary>
    /// Indicates if auto-sync is disabled
    /// </summary>
    public static readonly BindableProperty IsAutoSyncDisabledProperty =
        BindableProperty.Create(
            nameof(IsAutoSyncDisabled),
            typeof(bool),
            typeof(SyncStatusBar),
            false);

    public bool IsAutoSyncDisabled
    {
        get => (bool)GetValue(IsAutoSyncDisabledProperty);
        set => SetValue(IsAutoSyncDisabledProperty, value);
    }

    #endregion
}
