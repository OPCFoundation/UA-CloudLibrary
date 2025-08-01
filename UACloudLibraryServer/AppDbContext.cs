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

using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class AppDbContext : IdentityDbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(DbContextOptions options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

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
            string connectionString = configuration.GetConnectionString("CloudLibraryPostgreSQL");

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

        public DbSet<NamespaceMetaDataModel> NamespaceMetaDataWithUnapproved { get; set; }

        public DbSet<CloudLibNodeSetModel> NodeSetsWithUnapproved { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            NodeSetModelContext.CreateModel(builder, true);

            builder.Entity<CloudLibNodeSetModel>()
                .Property(nsm => nsm.ValidationStatus)
                .HasConversion<string>();

            builder.Entity<NodeSetModel>()
                .Ignore(nsm => nsm.HeaderComments)
                .HasAlternateKey(nm => nm.Identifier);

            builder.Entity<NodeModel>()
                .Ignore(nm => nm.AllReferencedNodes);

            builder.Entity<NodeModel>()
                .HasIndex(nm => new { nm.BrowseName })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");

            builder.Entity<NamespaceMetaDataModel>()
                .Ignore(md => md.ValidationStatus)
                .HasKey(n => n.NodesetId);

            builder.Entity<NamespaceMetaDataModel>()
                .Property(nsm => nsm.ApprovalStatus)
                .HasConversion<string>();

            builder.Entity<NamespaceMetaDataModel>()
                .HasOne(md => md.NodeSet)
                .WithOne(nm => nm.Metadata)
                .HasForeignKey<CloudLibNodeSetModel>(nm => nm.Identifier)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<NamespaceMetaDataModel>()
                .HasIndex(md => new { md.Title, md.Description })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");

            builder.Entity<DbFiles>();
        }
    }
}
