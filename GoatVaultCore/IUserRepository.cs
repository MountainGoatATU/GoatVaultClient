using GoatVaultCore.Models;

namespace GoatVaultCore;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email);
    Task<User?> GetByIdAsync(Guid id);
    Task SaveAsync(User user);
}