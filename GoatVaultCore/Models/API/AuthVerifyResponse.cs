namespace GoatVaultCore.Models.Api;

public class AuthVerifyResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}