using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultInfrastructure.Database;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(Email email) => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id) => db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task SaveAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
