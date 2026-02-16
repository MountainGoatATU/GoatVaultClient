using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserRequest
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
}