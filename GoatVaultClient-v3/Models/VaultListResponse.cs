using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class VaultListResponse
    {
        public List<VaultPayload>? Vaults { get; set; }
    }
}
