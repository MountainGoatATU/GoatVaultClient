
using GoatVaultInfrastructure.Services.Vault;
using System.Runtime.CompilerServices;

namespace GoatVaultClient.Services
{
    public class SyncingService
    {
        private readonly VaultService _vaultService;
        private readonly VaultSessionService _vaultSessionService;
        private readonly ConnectivityService _connectivityService;
        public SyncingService(VaultService vaultService, VaultSessionService vaultSessionService, ConnectivityService connectivityService) 
        {
            _vaultService = vaultService;
            _vaultSessionService = vaultSessionService;
            _connectivityService = connectivityService;
        }

        
    }
}
