using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class AuthLogoutResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
}