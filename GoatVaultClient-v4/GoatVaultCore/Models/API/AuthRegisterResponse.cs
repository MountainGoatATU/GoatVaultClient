using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRegisterResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    [JsonPropertyName("email")] public required string Email { get; set; }
    [JsonPropertyName("created_at")] public required DateTime CreatedAt { get; set; }
}