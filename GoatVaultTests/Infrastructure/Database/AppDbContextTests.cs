using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using GoatVaultInfrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultTests.Infrastructure.Database;

public class AppDbContextTests
{
    [Fact]
    public void SaveAndLoadUser_PersistsEmailConversionAndOwnedVaultFields()
    {
        using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);

        using (var setup = new AppDbContext(options))
        {
            setup.Database.EnsureCreated();
        }

        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "alice@example.com");

        using (var write = new AppDbContext(options))
        {
            write.Users.Add(user);
            write.SaveChanges();
        }

        using var read = new AppDbContext(options);
        var loaded = read.Users.Single(u => u.Id == userId);

        Assert.Equal("alice@example.com", loaded.Email.Value);
        Assert.Equal(user.Vault.EncryptedBlob, loaded.Vault.EncryptedBlob);
        Assert.Equal(user.Vault.Nonce, loaded.Vault.Nonce);
        Assert.Equal(user.Vault.AuthTag, loaded.Vault.AuthTag);
        Assert.Equal(user.Argon2Parameters.TimeCost, loaded.Argon2Parameters.TimeCost);
        Assert.Equal(user.Argon2Parameters.MemoryCost, loaded.Argon2Parameters.MemoryCost);
        Assert.Equal(user.Argon2Parameters.HashLength, loaded.Argon2Parameters.HashLength);
    }

    [Fact]
    public void ModelConfiguration_UsesExpectedColumnNames()
    {
        using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);

        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Users');";

        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        Assert.Contains("_id", columns);
        Assert.Contains("VaultEncryptedBlob", columns);
        Assert.Contains("VaultNonce", columns);
        Assert.Contains("VaultAuthTag", columns);
        Assert.Contains("Argon2TimeCost", columns);
        Assert.Contains("Argon2MemoryCost", columns);
        Assert.Contains("Argon2Lanes", columns);
        Assert.Contains("Argon2Threads", columns);
        Assert.Contains("Argon2HashLength", columns);
    }

    private static SqliteConnection CreateInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

    private static User CreateUser(Guid id, string email) => new()
    {
        Id = id,
        Email = new Email(email),
        AuthSalt = [1, 2, 3],
        AuthVerifier = [4, 5, 6],
        MfaEnabled = false,
        MfaSecret = [7, 8],
        ShamirEnabled = false,
        Argon2Parameters = new Argon2Parameters
        {
            TimeCost = 4,
            MemoryCost = 131072,
            Lanes = 2,
            Threads = 2,
            HashLength = 32
        },
        VaultSalt = [9, 10],
        Vault = new VaultEncrypted(
            encryptedBlob: [11, 12, 13],
            nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            authTag: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
        {
            EncryptedBlob = [11, 12, 13],
            Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
        },
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
