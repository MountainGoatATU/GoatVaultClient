using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class AuthInitRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
}