using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions options)
        : base(options)
        {
        }

        // Needed for design-time DB migration
        public AppDbContext()
        {
        }

        // Needed for design-time DB migration
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .Build();

                string connectionString = "Please set connection string here during design time migration as env variables are not available!";
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        // map to our tables
        public DbSet<Datatype> DataType { get; set; }
        
        public DbSet<Metadata> Metadata { get; set; }
        
        public DbSet<Objecttype> ObjectType { get; set; }

        public DbSet<Referencetype> ReferenceType { get; set; }
         
        public DbSet<Variabletype> VariableType { get; set; }
                
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Datatype>();
            modelBuilder.Entity<Metadata>();
            modelBuilder.Entity<Objecttype>();
            modelBuilder.Entity<Referencetype>();
            modelBuilder.Entity<Variabletype>();
        }
    }
}
