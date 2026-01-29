using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthLogoutResponse
{
    [JsonPropertyName("status")] public string Status { get; set; }
}