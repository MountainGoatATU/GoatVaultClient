namespace GoatVaultClient.Services;

public interface INetworkService
{
    bool IsConnected();
}

public class NetworkService : INetworkService
{
    public bool IsConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}