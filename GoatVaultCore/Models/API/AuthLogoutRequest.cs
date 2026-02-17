namespace GoatVaultCore.Models.API;

public class AuthLogoutRequest
{
    public required string RefreshToken { get; set; }
}