using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient.Models
{
    public class VaultListResponse
    {
        public List<VaultPayload> vaults { get; set; }
    }
}
