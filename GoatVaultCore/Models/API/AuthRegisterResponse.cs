using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRegisterResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    public required string Email { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}