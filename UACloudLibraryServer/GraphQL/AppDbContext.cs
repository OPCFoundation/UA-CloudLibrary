/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.IO;
    using System.Linq;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.EF;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua.Cloud.Library.DbContextModels;

    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions options, IConfiguration configuration)
: base(options)
        {
            _configuration = configuration;
            _approvalRequired = configuration.GetSection("CloudLibrary")?.GetValue<bool>("ApprovalRequired") ?? false;
        }

        private readonly IConfiguration _configuration;
        private readonly bool _approvalRequired;


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
            if (!optionsBuilder.IsConfigured)
            {
                IConfiguration configuration = _configuration;
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                       .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json")
                       .Build();
                }
                string connectionString = CreateConnectionString(configuration);
                optionsBuilder
                    .UseNpgsql(connectionString, o => o
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                        .EnableRetryOnFailure()
                        )
                    ;
            }
        }

        public static string CreateConnectionString(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CloudLibraryPostgreSQL");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = CreateConnectionStringFromEnvironment();
            }
            return connectionString;
        }

        private static string CreateConnectionStringFromEnvironment()
        {
            // Obtain connection string information from the environment
            string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

            string DBname = "uacloudlib";
            string Port = "5432";

            // Build connection string using parameters from portal
            return $"Server={Host};Username={User};Database={DBname};Port={Port};Password={Password};SSLMode=Prefer";
        }



        // map to our tables

        public IQueryable<NamespaceMetaDataModel> NamespaceMetaData
        {
            get => _approvalRequired
                ? NamespaceMetaDataWithUnapproved.Where(n => n.ApprovalStatus == ApprovalStatus.Approved)
                : NamespaceMetaDataWithUnapproved;
        }

        // Full metadata dbset, use only for Add
        public DbSet<NamespaceMetaDataModel> NamespaceMetaDataWithUnapproved { get; set; }

#if !NOLEGACYMIGRATION
        public DbSet<MetadataModel> LegacyMetadata { get; set; }
#endif
        public DbSet<OrganisationModel> Organisations { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }


        // nodeSet query filtered to only approved nodesets: use for all access
        public IQueryable<CloudLibNodeSetModel> nodeSets
        {
            get =>
                _approvalRequired
                ? nodeSetsWithUnapproved.Where(n => NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == n.Identifier && nmd.ApprovalStatus == ApprovalStatus.Approved))
                : nodeSetsWithUnapproved;
        }

        // Full dbset, use only for Add or administrator-protected queries
        public DbSet<CloudLibNodeSetModel> nodeSetsWithUnapproved { get; set; }


        // nodeModel query filtered to only approved nodesets: use for all access
        public IQueryable<NodeModel> nodeModels
        {
            get =>
                _approvalRequired
                ? nodeModelsWithUnapproved.Where(n => NamespaceMetaData.Any(nmd => nmd.NodesetId == n.NodeSet.Identifier && nmd.ApprovalStatus == ApprovalStatus.Approved))
                : nodeModelsWithUnapproved;
        }

        // Full dbset, use only for Add or administrator-protected queries
        public DbSet<NodeModel> nodeModelsWithUnapproved { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            NodeSetModelContext.CreateModel(builder, true);
            builder.Entity<CloudLibNodeSetModel>()
                .Property(nsm => nsm.ValidationStatus)
                    .HasConversion<string>();
            builder.Entity<NodeSetModel>()
                .HasAlternateKey(nm => nm.Identifier)
                ;

            builder.Entity<NodeModel>()
                .Ignore(nm => nm.AllReferencedNodes);

            builder.Entity<NodeModel>()
                .HasIndex(nm => new { nm.BrowseName })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english")
                ;
            NamespaceMetaDataModel.OnModelCreating(builder);
#if !NOLEGACYMIGRATION
            builder.Entity<MetadataModel>().HasKey(k => k.Id);
#endif
            DevDbFileStorage.OnModelCreating(builder);
        }

    }
}
