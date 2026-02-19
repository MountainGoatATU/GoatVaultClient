using Microsoft.Extensions.Logging;

namespace GoatVaultClient.Services;

/// <summary>
/// Service for managing network connectivity state across the application
/// </summary>
public class ConnectivityService : IDisposable
{
    private readonly ILogger<ConnectivityService>? _logger;
    private bool _isDisposed;

    public event EventHandler<bool>? ConnectivityChanged;

    public bool IsConnected { get; private set; }

    public ConnectivityService(ILogger<ConnectivityService>? logger = null)
    {
        _logger = logger;

        // Set initial state
        IsConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        // Subscribe to connectivity changes
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var wasConnected = IsConnected;
        IsConnected = e.NetworkAccess == NetworkAccess.Internet;

        if (wasConnected == IsConnected)
            return;

        _logger?.LogInformation("Connectivity changed: {IsOnline}", IsConnected);
        ConnectivityChanged?.Invoke(this, IsConnected);
    }

    /// <summary>
    /// Check if there is internet connectivity
    /// </summary>
    public bool CheckConnectivity()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            IsConnected = networkAccess == NetworkAccess.Internet;
            return IsConnected;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking connectivity");
            return false;
        }
    }

    /// <summary>
    /// Get detailed network information
    /// </summary>
    public NetworkInfo GetNetworkInfo()
    {
        var profiles = Connectivity.Current.ConnectionProfiles;
        var networkAccess = Connectivity.Current.NetworkAccess;

        return new NetworkInfo
        {
            IsConnected = networkAccess == NetworkAccess.Internet,
            NetworkAccess = networkAccess,
            ConnectionProfiles = profiles.ToList()
        };
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _isDisposed = true;
    }
}

/// <summary>
/// Detailed network information
/// </summary>
public class NetworkInfo
{
    public bool IsConnected { get; set; }
    public NetworkAccess NetworkAccess { get; set; }
    public List<ConnectionProfile> ConnectionProfiles { get; set; } = [];

    public string GetConnectionType()
    {
        if (ConnectionProfiles.Contains(ConnectionProfile.WiFi))
            return "WiFi";
        if (ConnectionProfiles.Contains(ConnectionProfile.Cellular))
            return "Cellular";
        if (ConnectionProfiles.Contains(ConnectionProfile.Ethernet))
            return "Ethernet";
        return ConnectionProfiles.Contains(ConnectionProfile.Bluetooth)
            ? "Bluetooth"
            : "Unknown";
    }
}
