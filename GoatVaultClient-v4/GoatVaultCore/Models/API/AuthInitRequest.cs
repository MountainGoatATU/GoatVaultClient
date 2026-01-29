using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthInitRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
}