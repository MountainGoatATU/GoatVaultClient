using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthLogoutResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
}