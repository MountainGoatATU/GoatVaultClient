using GoatVaultCore.Models.Shamir;
using GoatVaultCore.Services.Shamir;
using System.Collections.ObjectModel;

namespace GoatVaultApplication.Shamir;

public class SplitKeyUseCase
{
    private readonly IShamirSSService _shamirSSService;
    public SplitKeyUseCase(IShamirSSService shamirSSService)
    {
        _shamirSSService = shamirSSService;
    }
    public ObservableCollection<RecoveryShare> Execute(string secret, string passPhrase, int totalShares, int threshold)
    {
        ObservableCollection<RecoveryShare> generatedShares = new ObservableCollection<RecoveryShare>();

        var splittedKey =  _shamirSSService.SplitSecret(secret, passPhrase, totalShares, threshold);

        for (var i = 0; i < splittedKey.Count; i++)
        {
            generatedShares.Add(new RecoveryShare
            {
                Index = i + 1,
                Mnemonic = splittedKey[i]
            });
        }

        return generatedShares;
    }
}
