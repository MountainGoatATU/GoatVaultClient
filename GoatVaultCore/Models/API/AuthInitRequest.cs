using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthInitRequest
{
    public required string Email { get; set; }
}