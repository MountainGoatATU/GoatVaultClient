using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.Api;

public class UserRequest
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
}