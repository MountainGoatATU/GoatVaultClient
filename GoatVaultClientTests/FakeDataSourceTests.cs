using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClientTests
{
    [TestFixture]
    public class FakeDataSourceTests
    {
        private FakeDataSource _fakeDataSource;
        [SetUp]
        public void Setup()
        {
            _fakeDataSource = new FakeDataSource();
        }

        [Test]
        public void GetFolderItems_ReturnsAllCategories()
        {
            // Arrange
            var expectedCategories = new List<CategoryItem>
            {
                new CategoryItem { Name = "Work" },
                new CategoryItem { Name = "Personal" },
                new CategoryItem { Name = "Finance" },
                new CategoryItem { Name = "Health" },
                new CategoryItem { Name = "Travel" }
            };
            // Act
            var folderItems = _fakeDataSource.GetFolderItems();
            // Assert
            Assert.That(folderItems, Is.Not.Null);
            Assert.That(folderItems.Count, Is.EqualTo(expectedCategories.Count)); // As per defined categories
            foreach (var expected in expectedCategories)
            {
                Assert.That(folderItems.Any(f => f.Name == expected.Name), Is.True, $"Category '{expected.Name}' not found.");
            }
        }

        [Test]
        public void GetVaultEntryItems_ReturnsSpecifiedCount()
        {
            // Arrange
            int count = 10;
            // Act
            var vaultEntries = _fakeDataSource.GetVaultEntryItems(count);
            // Assert
            Assert.That(vaultEntries, Is.Not.Null);
            Assert.That(vaultEntries.Count, Is.EqualTo(count));
            foreach (var entry in vaultEntries)
            {
                Assert.That(entry.Site, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.UserName, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.Password, Is.Not.Null.And.Not.Empty);
                Assert.That(entry.Description, Is.Not.Null.And.Not.Empty);
                Assert.That(new List<string> { "Work", "Personal", "Finance", "Health", "Travel" }, Does.Contain(entry.Category));
            }
        }
    }
}
