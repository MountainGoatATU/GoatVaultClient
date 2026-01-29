namespace GoatVaultClient.Services;

public interface IAuthTokenService
{
    string? GetToken();
    void SetToken(string token);
    void ClearToken();
}

public class AuthTokenService : IAuthTokenService
{
    private string? _token;

    public string? GetToken() => _token;

    public void SetToken(string token) => _token = token;

    public void ClearToken() => _token = null;
}