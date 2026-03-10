using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultInfrastructure.Database;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(Email email) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<List<User>> GetAllAsync() => db.Users.AsNoTracking().ToListAsync();

    public async Task SaveAsync(User user)
    {
        // Detach any existing tracked User entity with the same Id
        var existingEntry = db.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Id == user.Id);

        existingEntry?.State = EntityState.Detached;

        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == user.Id);

        if (exists)
            db.Users.Update(user);
        else
            db.Users.Add(user);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        // Detach any existing tracked User entity with the same Id
        var existingEntry = db.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Id == user.Id);

        existingEntry?.State = EntityState.Detached;

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}
