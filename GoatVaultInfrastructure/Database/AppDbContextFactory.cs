using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoatVaultInfrastructure.Database;

// ReSharper disable once UnusedMember.Global
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var dbPath = Path.Combine(AppContext.BaseDirectory, "goatvault-migrations.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new AppDbContext(optionsBuilder.Options);
    }
}
