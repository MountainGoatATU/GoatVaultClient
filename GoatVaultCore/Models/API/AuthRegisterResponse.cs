using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.Api;

public class AuthRegisterResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    public required string Email { get; set; }
}