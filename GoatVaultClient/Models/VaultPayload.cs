using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient.Models
{
    public class VaultPayload
    {
        public string _id { get; set; }
        public string user_id { get; set; }
        public string name { get; set; }
        public string salt { get; set; }
        public string nonce { get; set; }
        public string encrypted_blob { get; set; }
        public string auth_tag { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
