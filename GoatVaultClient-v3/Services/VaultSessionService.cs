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
        public VaultData DecryptedVault { get; set; } = null;
        public UserResponse CurrentUser { get; set; } = null;
        public string? MasterPassword { get; set; } = "";
        
        public void Lock()
        {
            DecryptedVault = null;
            CurrentUser = null;
            MasterPassword = null;

            // Force Garbage Collection to remove secrets from heap (Optional but good)
            GC.Collect();
        }
    }
}
