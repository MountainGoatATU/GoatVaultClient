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
    public class GoatVaultDB : DbContext
    {
        public DbSet<DbModel> LocalCopy { get; set; }

        public GoatVaultDB(DbContextOptions<GoatVaultDB> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbModel>()
                .HasKey(v => v.Id);


            modelBuilder.Entity<DbModel>(entityBuilder =>
            {
                entityBuilder.Property(b => b.Id).HasColumnName("_id");
            });
        }
    }
}
