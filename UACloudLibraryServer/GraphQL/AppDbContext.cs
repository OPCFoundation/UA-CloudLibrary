using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using UACloudLibrary;
using System.Linq;
using System;
using UA_CloudLibrary.DbContextModels;

namespace UA_CloudLibrary.GraphQL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Organisation> Organisations { get; set; }

        public DbSet<AddressSpace> AddressSpaces { get; set; }

        public DbSet<AddressSpaceCategory> AddressSpaceCategories { get; set; }

        public DbSet<AddressSpaceNodeset2> AddressSpaceNodesets { get; set; }

        public DbSet<Objecttype> ObjectTypes { get; set; }

        public DbSet<Referencetype> ReferenceTypes { get; set; }

        public DbSet<Nodeset> Nodeset { get; set; }

        public DbSet<Datatype> DataTypes { get; set; }

        public DbSet<Variabletype> VariableTypes { get; set; }

        public DbSet<Metadata> Metadata { get; set; }


        public static IModel GetInstance()
        {
            string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

            string DBname = "uacloudlib";
            string Port = "5432";

            string _connectionString = string.Format(
                "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                Host,
                User,
                DBname,
                Port,
                Password);

            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(_connectionString);
            using AppDbContext context = new AppDbContext(builder.Options);

            return context.Model;
        }


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
