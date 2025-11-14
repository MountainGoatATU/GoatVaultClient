using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class UserPayload
    {
        public string? Email { get; set; }
        public string? Salt { get; set; }
        public string? Password_hash { get; set; }
        public bool? Mfa_enabled { get; set; }
        public string? Mfa_secret { get; set; }
    }
}
