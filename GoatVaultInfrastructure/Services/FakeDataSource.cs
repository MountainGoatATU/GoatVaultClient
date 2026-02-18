using Bogus;
using GoatVaultCore.Models;

namespace GoatVaultInfrastructure.Services;

public class FakeDataSource
{
    private readonly Faker<VaultEntry> _vaultEntryFaker = new Faker<VaultEntry>()
        .RuleFor(f => f.Site, f => f.Company.CompanyName())
        .RuleFor(f => f.UserName, f => f.Internet.Email())
        .RuleFor(f => f.Password, f => f.Internet.Password())
        .RuleFor(f => f.Description, f => f.Lorem.Sentence())
        .RuleFor(f => f.Category, f => f.PickRandom(new List<string> { "Work", "Personal", "Finance", "Health", "Travel", }));

    public List<CategoryItem> GetFolderItems()
    {
        string[] categories = ["Work", "Personal", "Finance", "Health", "Travel"];
        List<CategoryItem> folderItems = [];

        folderItems.AddRange(categories
            .Select(c => new CategoryItem { Name = c }));

        return folderItems;
    }

    public List<VaultEntry> GetVaultEntryItems(int count) => _vaultEntryFaker.Generate(count);
}