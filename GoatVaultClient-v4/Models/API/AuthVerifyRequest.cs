using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthVerifyRequest
{
    [JsonPropertyName("_id")] public Guid UserId { get; set; }
    [JsonPropertyName("auth_verifier")] public string AuthVerifier { get; set; }
}