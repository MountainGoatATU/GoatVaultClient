using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthInitRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
}