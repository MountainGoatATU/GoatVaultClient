namespace GoatVaultCore.Services.Shamir;

public interface IShamirSSService
{
    public List<string> SplitSecret(string secret, string passPhrase, int totalShares, int threshold);
    public string RecoverSecret(List<string> mnemonicShares, string passphrase);
}
