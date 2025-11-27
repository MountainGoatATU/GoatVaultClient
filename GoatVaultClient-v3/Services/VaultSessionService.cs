using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.Services
{
    public class VaultSessionService
    {
        public VaultData DecryptedVault { get; set; }

        public void Lock()
        {
            DecryptedVault = null;

            // Force Garbage Collection to remove secrets from heap (Optional but good)
            GC.Collect();
        }
    }
}
