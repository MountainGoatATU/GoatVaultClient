using GoatVaultCore.Models.Vault;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API
{
    public class UserUpdateRequest
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("mfa_enabled")] public bool? MfaEnabled { get; set; }
        [JsonPropertyName("vault")] public VaultModel? Vault { get; set; }
    }
}
