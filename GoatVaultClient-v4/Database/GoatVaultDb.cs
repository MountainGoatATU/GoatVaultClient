using Microsoft.EntityFrameworkCore;
using GoatVaultClient_v4.Models;

namespace GoatVaultClient_v4.Database;

public class GoatVaultDb(DbContextOptions<GoatVaultDb> options) : DbContext(options)
{
    public DbSet<DbModel> LocalCopy { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbModel>()
            .HasKey(v => v.Id);


        modelBuilder.Entity<DbModel>(entityBuilder => entityBuilder
            .Property(b => b.Id)
            .HasColumnName("_id"));
    }
}