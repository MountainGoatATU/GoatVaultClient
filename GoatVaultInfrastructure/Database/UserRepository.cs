using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultInfrastructure.Database;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(Email email) => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) => db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<List<User>> GetAllAsync() => db.Users.ToListAsync();

    public async Task SaveAsync(User user)
    {
        if (db.Users.Any(u => u.Id == user.Id))
        {
            db.Users.Update(user);
        }
        else
        {
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
