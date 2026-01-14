using Bogus;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.Services
{
    public class FakeDataSource
    {
        private readonly Faker<VaultEntry> vaultEntryFaker = new Faker<VaultEntry>()
            .RuleFor(f => f.Site, f => f.Company.CompanyName())
            .RuleFor(f => f.UserName, f => f.Internet.Email())
            .RuleFor(f => f.Password, f => f.Internet.Password())
            .RuleFor(f => f.Description, f => f.Lorem.Sentence())
            .RuleFor(f => f.Category, f => f.PickRandom(new List<string> { "Work", "Personal", "Finance", "Health", "Travel", }));

        public List<CategoryItem> GetFolderItems()
        {
            string[] categories = { "Work", "Personal", "Finance", "Health", "Travel", };
            List<CategoryItem> folderItems = new List<CategoryItem>();
            
            foreach (var c in categories)
            {
                folderItems.Add(new CategoryItem { Name = c });
            }

            return folderItems;
        }

        public List<VaultEntry> GetVaultEntryItems(int count)
        {
            return vaultEntryFaker.Generate(count);
        }
    }
}
