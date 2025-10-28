using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient.Models
{
    public class UserPayload
    {
        public string email { get; set; }
        public string salt { get; set; }
        public string password_hash { get; set; }
        public bool mfa_enabled { get; set; }
        public string mfa_secret { get; set; }
    }
}
