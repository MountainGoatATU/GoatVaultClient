using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthRegisterResponse
{
    [JsonPropertyName("_id")] public string Id { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
}