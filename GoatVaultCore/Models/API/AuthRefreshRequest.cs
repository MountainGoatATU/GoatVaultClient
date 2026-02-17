using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRefreshRequest
{
    public string RefreshToken { get; set; }
}