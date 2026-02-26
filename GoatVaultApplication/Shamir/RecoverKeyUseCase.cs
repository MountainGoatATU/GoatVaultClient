using GoatVaultCore.Services.Shamir;

namespace GoatVaultApplication.Shamir;

public class RecoverKeyUseCase
{
    private readonly IShamirSSService _shamirSSService;
    public RecoverKeyUseCase(IShamirSSService shamirSSService)
    {
        _shamirSSService = shamirSSService;
    }
    public async Task<string> Execute(List<string> mnemonicShares, string passphrase)
    {
        return await Task.Run(() => _shamirSSService.RecoverSecret(mnemonicShares, passphrase));
    }
}
