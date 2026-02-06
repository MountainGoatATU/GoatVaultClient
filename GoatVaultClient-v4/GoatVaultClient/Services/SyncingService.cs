using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services.API;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace GoatVaultClient.Services
{
    public interface ISyncingService
    {
        Task Sync();
        Task Save();
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
        public DateTime LastSaved { get; private set; }
        public async Task Sync()
        {
            // Get server URL from configuration
            var url = configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

            // Ensure user is logged in
            if (vaultSessionService.CurrentUser == null && vaultSessionService.DecryptedVault == null)
                throw new InvalidOperationException("No user is currently logged in.");

            try
            {
                var localCopy = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);

                // Pull the server stored vault
                var userResponse = await httpService.GetAsync<UserResponse>(
                $"{url}v1/users/{vaultSessionService.CurrentUser.Id}"
                );

                // Validating server response
                if (userResponse == null || localCopy == null)
                    throw new InvalidOperationException("Failed to retrieve user data to compare");

                // Compare timestamps
                var compareResult = DateTime.Compare(localCopy.UpdatedAt, userResponse.UpdatedAt);
                // If timestamps are equal, we are synced. If local is newer, prompt user to update server. If server is newer, prompt user to update local.
                if (compareResult == 0)
                    return; // Synced
                else if (compareResult > 0)
                {
                    var syncPopup = new PromptPopup(
                        "Sync collision",
                        "Local data are more recent than data on the server. Do you want to update the server?",
                        "Update");
                    await MopupService.Instance.PushAsync(syncPopup);
                    var result = await syncPopup.WaitForScan();

                    if (result)
                    {
                        // Retrieve local vault 
                        var localVault = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);
                        // Encrypt local vault
                        var encryptedVault =  vaultService.EncryptVault(vaultSessionService.MasterPassword, vaultSessionService.DecryptedVault);
                        // Create update request
                        var updateUserRequest = new UserRequest
                        {
                            Email = vaultSessionService.CurrentUser.Email,
                            Vault = encryptedVault,
                        };
                        // Send update request to server
                        var updateResult = await httpService.PatchAsync<UserResponse>(
                            $"{url}v1/users/{vaultSessionService.CurrentUser.Id}",
                            updateUserRequest
                        );
                        if (updateResult == null)
                            throw new InvalidOperationException("Failed to update server data");

                        var popup = new PromptPopup(
                        "Success",
                        "Data synced successfully",
                        "Ok");
                        await MopupService.Instance.PushAsync(popup);
                        var syncResult = await popup.WaitForScan();
                    }
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
        public async Task Save()
        {
            // Validation
            if (vaultSessionService.CurrentUser == null || vaultSessionService.DecryptedVault == null)
                return;
            try
            {
                // Update the decrypted vault in session
                VaultData updatedVault = new VaultData
                {
                    Categories = vaultSessionService.DecryptedVault.Categories,
                    Entries = vaultSessionService.DecryptedVault.Entries

                };
                // Create encrypted vault
                var encryptedVault = vaultService.EncryptVault(vaultSessionService.MasterPassword, updatedVault);
                // Retrieve the user from the database
                var localUser = await vaultService.LoadUserFromLocalAsync(vaultSessionService.CurrentUser.Id);
                // Update the local user
                localUser.Email = vaultSessionService.CurrentUser.Email;
                localUser.Vault = encryptedVault;
                localUser.UpdatedAt = DateTime.UtcNow;
                // Save user in local db
                await vaultService.UpdateUserInLocalAsync(localUser);
                // Update last saved property
                LastSaved = localUser.UpdatedAt;
            }
            catch (Exception ex)
            {

            }
           
        }
    }
    public enum SyncStatus
    {
        Synced,
        Unsynced,
    }
}
