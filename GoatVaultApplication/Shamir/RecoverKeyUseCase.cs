using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Shamir;

public class RecoverKeyUseCase(IShamirSsService shamir)
{
    public async Task<string> Execute(List<string> mnemonicShares, string passphrase) 
        => await Task.Run(() => shamir.RecoverSecret(mnemonicShares, passphrase));
}
