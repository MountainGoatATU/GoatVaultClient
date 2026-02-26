using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using GoatVaultInfrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultTests.Infrastructure.Database;

public class UserRepositoryTests
{
    [Fact]
    public async Task SaveAsync_NewUser_PersistsUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);
        await using var db = CreateContext(options);
        var repository = new UserRepository(db);
        var user = CreateUser(Guid.NewGuid(), "new@example.com");

        // Act
        await repository.SaveAsync(user);

        // Assert
        var fromDb = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Id, ct);
        Assert.NotNull(fromDb);
        Assert.Equal("new@example.com", fromDb.Email.Value);
    }

    [Fact]
    public async Task SaveAsync_ExistingUser_UpdatesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);

        // Act
        var userId = Guid.NewGuid();
        await using (var seedDb = CreateContext(options))
        {
            seedDb.Users.Add(CreateUser(userId, "before@example.com"));
            await seedDb.SaveChangesAsync(ct);
        }

        await using (var updateDb = CreateContext(options))
        {
            var repository = new UserRepository(updateDb);
            var user = await updateDb.Users.SingleAsync(u => u.Id == userId, ct);
            user.Email = new Email("after@example.com");

            await repository.SaveAsync(user);
        }

        await using var assertDb = CreateContext(options);
        var updated = await assertDb.Users.SingleAsync(u => u.Id == userId, ct);

        // Assert
        Assert.Equal("after@example.com", updated.Email.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);
        var user = CreateUser(Guid.NewGuid(), "byid@example.com");

        // Act
        await using (var seedDb = CreateContext(options))
        {
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync(ct);
        }

        await using var db = CreateContext(options);
        var repository = new UserRepository(db);

        var result = await repository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);
        var user = CreateUser(Guid.NewGuid(), "mail@example.com");

        // Act
        await using (var seedDb = CreateContext(options))
        {
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync(ct);
        }

        await using var db = CreateContext(options);
        var repository = new UserRepository(db);

        var result = await repository.GetByEmailAsync(new Email("mail@example.com"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);

        // Act
        await using (var seedDb = CreateContext(options))
        {
            seedDb.Users.Add(CreateUser(Guid.NewGuid(), "one@example.com"));
            seedDb.Users.Add(CreateUser(Guid.NewGuid(), "two@example.com"));
            await seedDb.SaveChangesAsync(ct);
        }

        await using var db = CreateContext(options);
        var repository = new UserRepository(db);

        var users = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var connection = CreateInMemoryConnection();
        var options = CreateOptions(connection);
        var user = CreateUser(Guid.NewGuid(), "delete@example.com");

        // Act
        await using (var seedDb = CreateContext(options))
        {
            seedDb.Users.Add(user);
            await seedDb.SaveChangesAsync(ct);
        }

        await using (var deleteDb = CreateContext(options))
        {
            var repository = new UserRepository(deleteDb);
            var toDelete = await deleteDb.Users.SingleAsync(u => u.Id == user.Id, ct);

            await repository.DeleteAsync(toDelete);
        }

        await using var assertDb = CreateContext(options);

        // Assert
        Assert.Empty(await assertDb.Users.ToListAsync(ct));
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

    private static AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
    {
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static User CreateUser(Guid id, string email) => new()
    {
        Id = id,
        Email = new Email(email),
        AuthSalt = [1, 2, 3],
        AuthVerifier = [4, 5, 6],
        MfaEnabled = false,
        MfaSecret = [7, 8],
        ShamirEnabled = false,
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
