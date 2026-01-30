using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthLogoutResponse
{
    [JsonPropertyName("status")] public required string Status { get; set; }
}