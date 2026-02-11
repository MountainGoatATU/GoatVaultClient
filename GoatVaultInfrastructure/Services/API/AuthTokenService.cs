namespace GoatVaultInfrastructure.Services.API;

public interface IAuthTokenService
{
    string? GetToken();
    string? GetRefreshToken();
    void SetToken(string token);
    void SetRefreshToken(string refreshToken);
    void ClearToken();
    void ClearRefreshToken();
}

public class AuthTokenService : IAuthTokenService
{
    private string? _token;
    private string? _refreshToken;
    private DateTime? _expiry;
    public string? GetToken() => _token;
    public string? GetRefreshToken() => _refreshToken;
    public void SetToken(string token) => _token = token;
    public void SetRefreshToken(string refreshToken) => _refreshToken = refreshToken;
    public void ClearToken() => _token = null;
    public void ClearRefreshToken() => _refreshToken = null;    
}