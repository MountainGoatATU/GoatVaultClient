namespace GoatVaultClient_v4.Services;

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