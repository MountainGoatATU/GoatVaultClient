using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Objects;
using System.Collections.ObjectModel;

namespace GoatVaultApplication.Shamir;

public class SplitKeyUseCase(IShamirSsService shamirSsService)
{
    public async Task<ObservableCollection<RecoveryShare>> Execute(string secret, string passPhrase, int totalShares, int threshold)
    {
        var generatedShares = new ObservableCollection<RecoveryShare>();

        var splitKey =  await Task.Run(() => shamirSsService.SplitSecret(secret, passPhrase, totalShares, threshold));

        for (var i = 0; i < splitKey.Count; i++)
        {
            generatedShares.Add(new RecoveryShare
            {
                Index = i + 1,
                Mnemonic = splitKey[i]
            });
        }

        return generatedShares;
    }
}
