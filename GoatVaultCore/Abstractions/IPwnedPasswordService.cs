namespace GoatVaultCore.Abstractions;

public interface IPwnedPasswordService
{
    Task<int?> CheckPasswordAsync(string password);
}
