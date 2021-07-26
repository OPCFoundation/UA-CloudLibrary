using Microsoft.EntityFrameworkCore;
using UACloudLibrary;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Setting up the required properties and the keys
            //modelBuilder
            //    .Entity<AddressSpace>(c =>
            //    {
            //        c.HasKey(key => key.ID);
            //        c.Property(p => p.Title)
            //            .IsRequired();
            //        c.Property(p => p.Version)
            //            .IsRequired();
            //        c.Property(p => p.License)
            //            .IsRequired();
            //        c.Property(p => p.Contributor)
            //            .IsRequired();
            //        c.Property(p => p.Category)
            //            .IsRequired();

            //        // Needs to be adjusted
            //        c.Property(p => p.Nodeset)
            //            .IsRequired();
            //        c.Property(p => p.Keywords)
            //            .IsRequired();
            //        c.Property(p => p.CreationTimeStamp)
            //            .IsRequired();
            //        c.Property(p => p.LastModification)
            //            .IsRequired();

            //        // Relationships
            //        c.HasOne(c => c.Contributor).WithOne(c => c.);
            //        c.HasOne(c => c.Category).WithOne("ID");
            //        c.HasOne(c => c.Nodeset).WithOne("AddressSpaceID");
            //    });

            //modelBuilder
            //    .Entity<Organisation>(c =>
            //    {
            //        c.HasKey(key => key.ID);
            //        c.Property(p => p.Name)
            //            .IsRequired();
            //        c.Property(p => p.ID)
            //            .IsRequired();
            //        c.Property(p => p.CreationTimeStamp)
            //            .IsRequired();
            //        c.Property(p => p.LastModification)
            //            .IsRequired();
            //    });

            //modelBuilder
            //    .Entity<AddressSpaceCategory>(c =>
            //    {
            //        c.HasKey(key => key.ID);
            //        c.Property(p => p.Name)
            //            .IsRequired();
            //        c.Property(p => p.ID)
            //            .IsRequired();
            //        c.Property(p => p.CreationTimeStamp)
            //            .IsRequired();
            //        c.Property(p => p.LastModification)
            //            .IsRequired();
            //    });

            //modelBuilder
            //    .Entity<AddressSpaceNodeset2>(c =>
            //    {
            //        c.HasKey(key => key.AddressSpaceID);
            //        c.Property(p => p.NodesetXml)
            //            .IsRequired();
            //        c.Property(p => p.AddressSpaceID)
            //            .IsRequired();
            //        c.Property(p => p.CreationTimeStamp)
            //            .IsRequired();
            //        c.Property(p => p.LastModification)
            //            .IsRequired();
            //    });
        }
    }
}
