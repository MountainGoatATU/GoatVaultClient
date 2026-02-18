using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.Api;

public class AuthVerifyRequest
{
    [JsonPropertyName("_id")] public required Guid UserId { get; set; }
    public required string? Proof { get; set; }
    public string? MfaCode { get; set; }
}