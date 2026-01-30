using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthVerifyRequest
{
    [JsonPropertyName("_id")] public required Guid UserId { get; set; }
    [JsonPropertyName("auth_verifier")] public required string AuthVerifier { get; set; }
}