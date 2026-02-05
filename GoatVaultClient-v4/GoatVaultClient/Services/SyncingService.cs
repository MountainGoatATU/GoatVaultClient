using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;

namespace GoatVaultClient.Services
{
    public interface ISyncingService
    {
        Task Sync();
        Task Save(ObservableCollection<CategoryItem> categories, ObservableCollection<VaultEntry> passwords, UserResponse user);
        bool MarkedAsChanged { get; }
        DateTime LastSynced { get; }
    }
    public class SyncingService (
        IConfiguration configuration,
        VaultSessionService vaultSessionService,
        VaultService vaultService,
        HttpService httpService,
        GoatVaultDb db): ISyncingService
    {
        public bool MarkedAsChanged { get; private set; }
        public DateTime LastSynced { get; private set; }
        public async Task Sync()
        {
            // Get server URL from configuration
            var url = configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;
            // Ensure user is logged in
            if (vaultSessionService.CurrentUser == null && vaultSessionService.DecryptedVault == null)
                throw new InvalidOperationException("No user is currently logged in.");
            try
            {
                var localCopy = vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);
                //Pull the server stored vault
                var userResponse = await httpService.GetAsync<UserResponse>(
                $"{url}v1/users/{vaultSessionService.CurrentUser.Id}"
                );
                // Validating server response
                if (userResponse == null || localCopy == null)
                    throw new InvalidOperationException("Failed to retrieve user data to compare");
                //Compare timestamps
                int compareResult = DateTime.Compare(localCopy.Result.UpdatedAt, userResponse.UpdatedAt);
                if (compareResult == 0)
                    return; // Synced
                else if (compareResult > 0)
                {
                    // Local is newer

                }
                else
                {
                    // Server is newer 
                }
            }
            catch
            {

            }
            finally
            {

            }
        }
        public async Task Save(ObservableCollection<CategoryItem> categories, ObservableCollection<VaultEntry> passwords, UserResponse user)
        {
            // Update the decrypted vault in session
            // Save the updated vault to local DB
        }
    }
    public enum SyncStatus
    {
        Synced,
        Unsynced,
    }
}
