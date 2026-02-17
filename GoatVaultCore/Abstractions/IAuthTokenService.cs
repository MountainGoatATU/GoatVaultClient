namespace GoatVaultCore.Abstractions;

public interface IAuthTokenService
{
    string GetToken();
    string GetRefreshToken();
    void SetToken(string token);
    void SetRefreshToken(string refreshToken);
    void ClearToken();
    void ClearRefreshToken();
}