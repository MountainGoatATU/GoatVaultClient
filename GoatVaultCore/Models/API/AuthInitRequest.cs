using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthInitRequest
{
    [JsonPropertyName("email")] public required string Email { get; set; }
}