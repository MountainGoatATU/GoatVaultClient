using GoatVaultInfrastructure.Services;

namespace GoatVaultTests;

public class FakeDataSourceTests
{
    private static readonly string[] ExpectedCategories =
    [
        "Work",
        "Personal",
        "Finance",
        "Health",
        "Travel"
    ];

    [Fact]
    public void GetFolderItems_ReturnsExpectedCategories()
    {
        // Arrange
        var dataSource = new FakeDataSource();

        // Act
        var folders = dataSource.GetFolderItems();

        // Assert
        Assert.NotNull(folders);
        Assert.Equal(ExpectedCategories.Length, folders.Count);

        Assert.Equal(
            ExpectedCategories.Order(),
            folders.Select(f => f.Name).Order()
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetVaultEntryItems_ReturnsCorrectCount_AndValidData(int count)
    {
        // Arrange
        var dataSource = new FakeDataSource();
        var validCategories = ExpectedCategories.ToHashSet();

        // Act
        var entries = dataSource.GetVaultEntryItems(count);

        // Assert
        Assert.NotNull(entries);
        Assert.Equal(count, entries.Count);

        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Site));
            Assert.False(string.IsNullOrWhiteSpace(entry.UserName));
            Assert.False(string.IsNullOrWhiteSpace(entry.Password));
            Assert.False(string.IsNullOrWhiteSpace(entry.Description));
            Assert.Contains(entry.Category, validCategories);
        }
    }
}