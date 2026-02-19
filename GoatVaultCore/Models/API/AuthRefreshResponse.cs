namespace GoatVaultCore.Models.Api;

public class AuthRefreshResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}