using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Microsoft.EntityFrameworkCore;

namespace GoatVaultInfrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);

                b.Property(u => u.Id)
                    .HasColumnName("_id");

                b.Property(u => u.Email)
                    .HasConversion(e => e.Value, s => new Email(s));

                b.OwnsOne(u => u.Vault, vault =>
                {
                    vault.Property(v => v.EncryptedBlob).HasColumnName("VaultEncryptedBlob");
                    vault.Property(v => v.Nonce).HasColumnName("VaultNonce");
                    vault.Property(v => v.AuthTag).HasColumnName("VaultAuthTag");
                });
            });
    }
}