namespace GoatVaultCore.Abstractions;

public interface IShamirSsService
{
    List<string> SplitSecret(string secret, string passPhrase, int totalShares, int threshold);
    string RecoverSecret(List<string> mnemonicShares, string passphrase);
}
