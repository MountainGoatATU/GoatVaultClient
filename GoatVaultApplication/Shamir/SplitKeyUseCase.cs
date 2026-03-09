using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Objects;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace GoatVaultApplication.Shamir;

public class SplitKeyUseCase(IShamirSsService shamirSsService, ILogger<SplitKeyUseCase>? logger = null)
{
    public async Task<ObservableCollection<RecoveryShare>> Execute(string secret, string passPhrase, int totalShares, int threshold)
    {
        try
        {
            logger?.LogInformation("SplitKeyUseCase.Execute called with totalShares={TotalShares}, threshold={Threshold}", totalShares, threshold);

            var generatedShares = new ObservableCollection<RecoveryShare>();

            var splitKey = await Task.Run(() => shamirSsService.SplitSecret(secret, passPhrase, totalShares, threshold));

            logger?.LogInformation("Successfully split secret into {ShareCount} shares", splitKey.Count);

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
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in SplitKeyUseCase.Execute");
            throw;
        }
    }
}
