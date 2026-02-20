using GoatVaultCore.Services.Shamir;

namespace GoatVaultApplication.Shamir;

public class SplitKeyUseCase
{
    private readonly IShamirSSService _shamirSSService;
    public SplitKeyUseCase(IShamirSSService shamirSSService)
    {
        _shamirSSService = shamirSSService;
    }
    public List<string> Execute(string secret, string passPhrase, int totalShares, int threshold)
    {
        return _shamirSSService.SplitSecret(secret, passPhrase, totalShares, threshold);
    }
}
