using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Services
{
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
}
