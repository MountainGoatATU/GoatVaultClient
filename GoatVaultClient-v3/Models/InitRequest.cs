using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class InitRequest
    {
        [JsonPropertyName("email")] public string Email { get; set; }
    }
}
