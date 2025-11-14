using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using GoatVaultClient_v3.Models;

namespace GoatVaultClient_v3.Database
{
    public class VaultDB : DbContext
    {
        public DbSet<VaultPayload> Vaults { get; set; }

        public VaultDB(DbContextOptions<VaultDB> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VaultPayload>()
                .HasKey(v => v.Id);


            modelBuilder.Entity<VaultPayload>(entityBuilder =>
            {
                entityBuilder.Property(b => b.Id).HasColumnName("_id");
            });
        }
    }
}
