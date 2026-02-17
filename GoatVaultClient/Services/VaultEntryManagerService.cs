using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.Vault;
using GoatVaultCore.Services.Secrets;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;

namespace GoatVaultClient.Services;

public class VaultEntryManagerService(
    VaultSessionService vaultSessionService,
    ISyncingService syncingService,
    PwnedPasswordService pwnedPasswordService,
    IPasswordStrengthService passwordStrengthService)
{
    public async Task<bool> CreateEntryAsync(IEnumerable<CategoryItem> categories)
    {
        var categoriesList = categories.ToList();

        var formModel = new VaultEntryForm(categoriesList, passwordStrengthService)
        {
            // Optional: Set a default selected category
            Category = categoriesList.FirstOrDefault()?.Name ?? ""
        };

        // Show the Auto-Generated Dialog
        var dialog = new VaultEntryDialog(formModel);

        await MopupService.Instance.PushAsync(dialog);
        await dialog.WaitForScan();

        // Simple validation
        if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
        {
            return false;
        }

        var newEntry = new VaultEntry
        {
            UserName = formModel.UserName,
            Site = formModel.Site,
            Password = formModel.Password,
            Description = formModel.Description,
            Category = formModel.Category,
            MfaSecret = formModel.MfaSecret,
            HasMfa = formModel.HasMfa
        };

        if (!string.IsNullOrEmpty(newEntry.Password))
        {
            newEntry.BreachCount = (int)await pwnedPasswordService.CheckPasswordAsync(newEntry.Password);
            
            // If breached, confirm with user before saving
            if (newEntry.BreachCount > 0)
            {
                var prompt = new PromptPopup(
                    "Breached Password",
                    $"This password was found in breach databases {newEntry.BreachCount} times. Do you want to save it anyway?",
                    "Save anyway",
                    "Back"
                );

                await MopupService.Instance.PushAsync(prompt);
                var keep = await prompt.WaitForScan();
                if (!keep)
                {
                    return false;
                }
            }
        }

        // Add to list
        vaultSessionService.DecryptedVault?.Entries.Add(newEntry);

        // Notify that entries changed
        vaultSessionService.RaiseVaultEntriesChanged();

        await syncingService.AutoSaveIfEnabled();

        return true;
    }

    public async Task<bool> EditEntryAsync(VaultEntry? target, IEnumerable<CategoryItem> categories)
    {
        if (target == null) return false;

        var entries = vaultSessionService.DecryptedVault?.Entries;
        if (entries == null) return false;

        var index = entries.IndexOf(target);
        if (index < 0) return false;

        var categoriesList = categories.ToList();

        var formModel = new VaultEntryForm(categoriesList, passwordStrengthService)
        {
            UserName = target.UserName,
            Site = target.Site,
            Password = target.Password,
            Description = target.Description,
            Category = target.Category,
            MfaSecret = target.MfaSecret,
            HasMfa = target.HasMfa
        };

        var dialog = new VaultEntryDialog(formModel);

        await MopupService.Instance.PushAsync(dialog);
        await dialog.WaitForScan();

        if (string.IsNullOrWhiteSpace(formModel.Site) || string.IsNullOrWhiteSpace(formModel.Password))
        {
            return false;
        }

        var updatedEntry = new VaultEntry
        {
            UserName = formModel.UserName,
            Site = formModel.Site,
            Password = formModel.Password,
            Description = formModel.Description,
            Category = formModel.Category,
            MfaSecret = formModel.MfaSecret,
            HasMfa = formModel.HasMfa
        };

        {
            updatedEntry.BreachCount = (int)await pwnedPasswordService.CheckPasswordAsync(updatedEntry.Password);

            // If breached, confirm with user before saving
            if (updatedEntry.BreachCount > 0)
            {
                var prompt = new PromptPopup(
                    "Breached Password",
                    $"This password was found in breach databases {updatedEntry.BreachCount} times. Do you want to save it anyway?",
                    "Save anyway",
                    "Back"
                );

                await MopupService.Instance.PushAsync(prompt);
                var keep = await prompt.WaitForScan();
                if (!keep)
                {
                    return false;
                }
            }
        }

        entries[index] = updatedEntry;
        // Notify that entries changed
        vaultSessionService.RaiseVaultEntriesChanged();

        await syncingService.AutoSaveIfEnabled();

        return true;
    }

    public async Task<bool> DeleteEntryAsync(VaultEntry? target)
    {
        if (target == null) return false;

        // Creating new prompt dialog
        var dialog = new PromptPopup("Confirm Delete", $"Are you sure you want to delete the password for \"{target.Site}\"?", "Delete");

        // Push the dialog to MopupService
        await MopupService.Instance.PushAsync(dialog);

        // Wait for the response
        var response = await dialog.WaitForScan();

        // Act based on the response
        if (!response)
            return false;

        // Remove from the list
        vaultSessionService.DecryptedVault?.Entries.Remove(target);

        // Notify that entries changed
        vaultSessionService.RaiseVaultEntriesChanged();

        await syncingService.AutoSaveIfEnabled();

        return true;
    }

    public static async Task CopyEntryPasswordAsync(VaultEntry? target)
    {
        if (target == null) return;

        await Clipboard.Default.SetTextAsync(target.Password);

        // Clear clipboard after 10 seconds (fire and forget task, but handled safely)
        _ = Task.Run(async () =>
        {
            await Task.Delay(10000);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var current = await Clipboard.Default.GetTextAsync();
                if (current == target.Password)
                {
                    await Clipboard.Default.SetTextAsync("");
                }
            });
        });
    }
}
