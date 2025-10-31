using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v2.Models
{
    public class VaultPayload
    {
        public string? _id { get; set; }
        public string? User_id { get; set; }
        public string? Name { get; set; }
        public string? Salt { get; set; }
        public string? Nonce { get; set; }
        public string? Encrypted_blob { get; set; }
        public string? Auth_tag { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
    }
}
