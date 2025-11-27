using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class PasswordItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; } = "\uf084";
        public string MaskedPassword => "••••••••••••";
    }
}
