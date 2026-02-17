namespace GoatVaultCore.Models.API;

public class AuthVerifyResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required string TokenType { get; set; }
}