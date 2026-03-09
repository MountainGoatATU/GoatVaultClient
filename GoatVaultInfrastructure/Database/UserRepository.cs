using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultInfrastructure.Database;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(Email email) => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<List<User>> GetAllAsync() => db.Users.ToListAsync();

    public async Task SaveAsync(User user)
    {
        var existingEntry = db.ChangeTracker.Entries<User>()
            .FirstOrDefault(e => e.Entity.Id == user.Id);

        if (existingEntry != null)
        {
            // Entity is already tracked, update its values
            existingEntry.CurrentValues.SetValues(user);
        }
        else if (await db.Users.AsNoTracking().AnyAsync(u => u.Id == user.Id))
        {
            // Entity exists in database but not tracked
            db.Users.Update(user);
        }
        else
        {
            // New entity
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }

    public Task DeleteAsync(User user)
    {
        db.Users.Remove(user);
        return db.SaveChangesAsync();
    }
}
