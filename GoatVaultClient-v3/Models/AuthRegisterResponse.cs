using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class RegisterResponse
    {
        [JsonPropertyName("_id")] public string Id { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
    }
}
