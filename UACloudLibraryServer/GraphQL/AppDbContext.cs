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

                string connectionString = "need to set connection string here during design time migration as env variables not available";
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        public DbSet<Organisation> Organisations { get; set; }

        public DbSet<AddressSpace> AddressSpaces { get; set; }

        public DbSet<AddressSpaceCategory> AddressSpaceCategories { get; set; }

        public DbSet<AddressSpaceNodeset2> AddressSpaceNodesets { get; set; }

        public DbSet<Objecttype> ObjectTypes { get; set; }

        public DbSet<Referencetype> ReferenceTypes { get; set; }

        public DbSet<Datatype> DataTypes { get; set; }

        public DbSet<Variabletype> VariableTypes { get; set; }

        public DbSet<Metadata> Metadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AddressSpace>().Ignore(c => c.AdditionalProperties);

            modelBuilder.Entity<AddressSpace>()
                .HasIndex(b => new { b.Title, b.Description, b.Keywords, b.SupportedLocales })
                .IsTsVectorExpressionIndex("english");
        }
    }
}
