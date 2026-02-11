using GoatVaultClient.Controls.Popups;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services.Vault;
using Mopups.Services;

namespace GoatVaultClient.Services;

public class CategoryManagerService(
    VaultSessionService vaultSessionService,
    ISyncingService syncingService)
{
    public async Task<bool> CreateCategoryAsync(IEnumerable<CategoryItem> existingCategories)
    {
        var popup = new SingleInputPopup("Create Category", "Category Name", "");

        await MopupService.Instance.PushAsync(popup);
        var result = await popup.WaitForScan();

        if (result == null)
            return false;

        var exists = existingCategories.Any(c => c.Name.Equals(result, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            // Create temp Category
            var temp = new CategoryItem { Name = result };

            // Add to global list
            vaultSessionService.DecryptedVault?.Categories.Add(temp);

            await syncingService.AutoSaveIfEnabled();

            return true;
        }
        else
        {
            // TODO: Implement error dialog or toast
            return false;
        }
    }

    public async Task<bool> EditCategoryAsync(CategoryItem? target)
    {
        if (target == null) return false;

        // Prevent renaming the built-in "All" category
        if (string.Equals(target.Name, "All", StringComparison.OrdinalIgnoreCase))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                "Cannot Rename",
                "The \"All\" category cannot be renamed.",
                "OK"
            ));
            return false;
        }

        // Find the index of the category in the vault
        var categories = vaultSessionService.DecryptedVault?.Categories;
        if (categories == null) return false;

        var index = categories.IndexOf(target);
        if (index < 0) return false;

        // Creating new prompt dialog
        var categoryPopup = new SingleInputPopup("Edit Category", "Category", target.Name);

        // Push the dialog to MopupService
        await MopupService.Instance.PushAsync(categoryPopup);

        // Wait for the response
        var response = await categoryPopup.WaitForScan();

        if (string.IsNullOrWhiteSpace(response)) return false;

        var oldName = target.Name;
        var reassign = false;

        // Check if any passwords use this category
        // We check the source of truth
        var vaultEntries = vaultSessionService.DecryptedVault?.Entries;
        if (vaultEntries == null) return false;

        var hasDependentPasswords = vaultEntries.Any(c => c.Category == oldName);

        if (hasDependentPasswords)
        {
            while (MopupService.Instance.PopupStack.Contains(categoryPopup))
                await Task.Delay(50);

            // Asking user to reassign the passwords
            var promptPopup = new PromptPopup("Reassign Passwords", $"Do you want to reassign passwords from \"{target.Name}\" to \"{response}\"?", "Accept");

            // Displaying dialog
            await MopupService.Instance.PushAsync(promptPopup);

            // Waiting for the response 
            reassign = await promptPopup.WaitForScan();
        }
        else
        {
            reassign = true;
        }

        // Update category name
        categories[index].Name = response;

        // Update passwords
        foreach (var pwd in vaultEntries.Where(c => c.Category == oldName))
        {
            pwd.Category = reassign ? response : string.Empty;
        }

        await syncingService.AutoSaveIfEnabled();

        return true;
    }

    public async Task<bool> DeleteCategoryAsync(CategoryItem? target)
    {
        if (target == null) return false;

        // Prevent deletion of the built-in "All" category
        if (string.Equals(target.Name, "All", StringComparison.OrdinalIgnoreCase))
        {
            await MopupService.Instance.PushAsync(new PromptPopup(
                "Cannot Delete",
                "The \"All\" category cannot be deleted.",
                "OK"
            ));
            return false;
        }

        // Creating new prompt dialog
        var categoryPopup = new PromptPopup("Confirm Delete", $"Are you sure you want to delete the \"{target.Name}\" category?", "Delete");

        // Push the dialog to MopupService
        await MopupService.Instance.PushAsync(categoryPopup);

        // Wait for the response
        var response = await categoryPopup.WaitForScan();

        if (!response) return false;

        var vaultEntries = vaultSessionService.DecryptedVault?.Entries;
        var categories = vaultSessionService.DecryptedVault?.Categories;

        if (vaultEntries?.Any(c => c.Category == target.Name) == true)
        {
            // Wait before pushing another dialog
            while (MopupService.Instance.PopupStack.Contains(categoryPopup))
                await Task.Delay(50);

            // Asking user to delete the passwords assign to the deleting category
            var promptPopup = new PromptPopup("Delete Passwords", $"Do you want to delete all passwords from \"{target.Name}\"?", "Delete");

            // Displaying dialog
            await MopupService.Instance.PushAsync(promptPopup);

            // Waiting for the response 
            var promptResponse = await promptPopup.WaitForScan();
            var passwordsToDelete = vaultEntries.Where(c => c.Category == target.Name).ToList();
            if (promptResponse)
            {
                foreach (var pwd in passwordsToDelete)
                {
                    vaultEntries.Remove(pwd);
                }
            }
        }

        // Remove category
        categories?.Remove(target);

        // Save the changes
        await syncingService.AutoSaveIfEnabled();

        return true;
    }
}
