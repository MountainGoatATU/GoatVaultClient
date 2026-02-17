using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllAsync();
    Task SaveAsync(User user);
    Task DeleteAsync(User user);
}