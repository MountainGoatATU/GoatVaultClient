using GoatVaultCore.Services.Shamir;

namespace GoatVaultApplication.Shamir;

public class RecoverKeyUseCase
{
    private readonly IShamirSSService _shamirSSService;
    public RecoverKeyUseCase(IShamirSSService shamirSSService)
    {
        _shamirSSService = shamirSSService;
    }
    public string Execute(List<string> mnemonicShares, string passphrase)
    {
        return _shamirSSService.RecoverSecret(mnemonicShares, passphrase);
    }
}
