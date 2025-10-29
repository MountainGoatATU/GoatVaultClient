using GoatVaultClient.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient.DB
{
    internal class VaultDB : DbContext
    {
        public DbSet<VaultPayload> Vaults { get; set; }

        public VaultDB(DbContextOptions<VaultDB> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VaultPayload>()
                .HasKey(v => v._id);
        }
    }
}
