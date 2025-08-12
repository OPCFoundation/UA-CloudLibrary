/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

                optionsBuilder.UseNpgsql(connectionString, o => o
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                    .EnableRetryOnFailure()
                ).LogTo(Console.WriteLine, LogLevel.Warning);
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

        public DbSet<NodeSetModel> NodeSetsWithUnapproved { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            CreateNodeModel(builder, true);

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
                .HasKey(n => n.NodesetId);

            builder.Entity<NamespaceMetaDataModel>()
                .Property(nsm => nsm.ApprovalStatus)
                .HasConversion<string>();

            builder.Entity<NamespaceMetaDataModel>()
                .HasOne(md => md.NodeSet)
                .WithOne(nm => nm.Metadata)
                .HasForeignKey<NodeSetModel>(nm => nm.Identifier)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<NamespaceMetaDataModel>()
                .HasIndex(md => new { md.Title, md.Description })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");

            builder.Entity<DbFiles>();
        }

        public void CreateNodeModel(ModelBuilder modelBuilder, bool cascadeDelete = false, bool methodArgs = false)
        {
            modelBuilder.Owned<NodeModel.LocalizedText>();
            modelBuilder.Owned<NodeModel.NodeAndReference>();
            modelBuilder.Owned<VariableModel.EngineeringUnitInfo>();
            modelBuilder.Owned<DataTypeModel.StructureField>();
            modelBuilder.Owned<DataTypeModel.UaEnumField>();
            modelBuilder.Owned<RequiredModelInfoModel>();

            modelBuilder.Entity<NodeSetModel>()
                .ToTable("NodeSets")
                .Ignore(nsm => nsm.AllNodesByNodeId)
                .Ignore(nsm => nsm.CustomState)
                .Ignore(nm => nm.NamespaceIndex)
                .HasKey(nsm => new { nsm.ModelUri, nsm.PublicationDate });

            var rmb = modelBuilder.Entity<NodeSetModel>()
                .OwnsMany(nsm => nsm.RequiredModels);

            rmb.WithOwner()
                .HasForeignKey("DependentModelUri", "DependentPublicationDate");

            if (cascadeDelete)
            {
                rmb.HasOne(rm => rm.AvailableModel).WithMany()
                    .OnDelete(DeleteBehavior.SetNull);
            }

            modelBuilder.Entity<NodeModel>()
                .Ignore(nm => nm.ReferencesNotResolved)
                .Ignore(nm => nm.NodeIdIdentifier)
                .Property<DateTime?>("NodeSetPublicationDate"); // EF tooling does not properly infer the type of this auto-generated property when using it in a foreign key: workaround declare explicitly

            modelBuilder.Entity<NodeModel>()
                .ToTable("Nodes")
                .HasKey(
                    nameof(NodeModel.NodeId),
                    $"{nameof(NodeModel.NodeSet)}{nameof(NodeSetModel.ModelUri)}",// Foreign key with auto-generated PK of the NodeModel.NodeSet property
                    $"{nameof(NodeModel.NodeSet)}{nameof(NodeSetModel.PublicationDate)}");

            modelBuilder.Entity<ObjectTypeModel>()
                .ToTable("ObjectTypes");

            var dtm = modelBuilder.Entity<DataTypeModel>()
                .ToTable("DataTypes");

            var vtm = modelBuilder.Entity<VariableTypeModel>()
                .ToTable("VariableTypes");

            var dvmParentFk = modelBuilder.Entity<DataVariableModel>()
                .ToTable("DataVariables")
                .HasOne(dv => dv.Parent).WithMany()
                .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate");

            if (cascadeDelete)
            {
                dvmParentFk.OnDelete(DeleteBehavior.Cascade);
            }

            var pmParentFk = modelBuilder.Entity<PropertyModel>()
                .ToTable("Properties")
                .HasOne(dv => dv.Parent).WithMany()
                .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate");

            if (cascadeDelete)
            {
                pmParentFk.OnDelete(DeleteBehavior.Cascade);
            }

            var omTd = modelBuilder.Entity<ObjectModel>()
                .ToTable("Objects");

            var omParentFk = modelBuilder.Entity<ObjectModel>()
                .HasOne(dv => dv.Parent).WithMany()
                .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate");

            if (cascadeDelete)
            {
                omParentFk.OnDelete(DeleteBehavior.Cascade);
            }

            modelBuilder.Entity<InterfaceModel>()
                .ToTable("Interfaces");

            modelBuilder.Entity<VariableModel>()
                .ToTable("Variables")
                .OwnsOne(v => v.EngineeringUnit).Property(v => v.NamespaceUri).IsRequired();

            var btmSt = modelBuilder.Entity<BaseTypeModel>()
                .ToTable("BaseTypes");

            modelBuilder.Entity<DataTypeModel>()
                .ToTable("DataTypes");

            modelBuilder.Entity<ObjectTypeModel>()
                .ToTable("ObjectTypes");

            modelBuilder.Entity<InterfaceModel>()
                .ToTable("Interfaces");

            modelBuilder.Entity<VariableTypeModel>()
                .ToTable("VariableTypes");

            modelBuilder.Entity<ReferenceTypeModel>()
                .ToTable("ReferenceTypes");

            if (!methodArgs)
            {
                modelBuilder.Entity<MethodModel>()
                    .Ignore(m => m.InputArguments)
                    .Ignore(m => m.OutputArguments);
            }

            var mmParentFk = modelBuilder.Entity<MethodModel>()
                .ToTable("Methods")
                .HasOne(dv => dv.Parent).WithMany()
                .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate");

            if (cascadeDelete)
            {
                mmParentFk.OnDelete(DeleteBehavior.Cascade);
            }

            modelBuilder.Entity<ReferenceTypeModel>()
                .ToTable("ReferenceTypes");

            DeclareNodeSetCollection(modelBuilder, nsm => nsm.ObjectTypes, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.VariableTypes, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.DataTypes, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.ReferenceTypes, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.Objects, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.Methods, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.Interfaces, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.Properties, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.DataVariables, cascadeDelete);
            DeclareNodeSetCollection(modelBuilder, nsm => nsm.UnknownNodes, cascadeDelete);

            // Unclear why these collection require declarations while the others just work
            modelBuilder.Entity<DataVariableModel>()
                .HasMany(dv => dv.NodesWithDataVariables).WithMany(nm => nm.DataVariables);

            modelBuilder.Entity<NodeModel>()
                .HasMany(nm => nm.Properties).WithMany(v => v.NodesWithProperties);

            modelBuilder.Entity<NodeModel>()
                .HasMany(nm => nm.Interfaces).WithMany(v => v.NodesWithInterface);

            {
                var orn = modelBuilder.Entity<NodeModel>()
                    .OwnsMany(nm => nm.OtherReferencedNodes);

                orn.WithOwner()
                    .HasForeignKey("OwnerNodeId", "OwnerModelUri", "OwnerPublicationDate");

                orn.Property<string>("ReferencedNodeId");
                orn.Property<string>("ReferencedModelUri");
                orn.Property<DateTime?>("ReferencedPublicationDate");

                var ornFK = orn.HasOne(nr => nr.Node).WithMany()
                    .HasForeignKey("ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate");

                if (cascadeDelete)
                {
                    ornFK.OnDelete(DeleteBehavior.Cascade);
                }

                orn.Property<string>("OwnerNodeId");
                orn.Property<string>("OwnerModelUri");
                orn.Property<DateTime?>("OwnerPublicationDate");

                //orn.Ignore(nr => nr.ReferenceType);
                orn.Property<string>("ReferenceTypeNodeId");
                orn.Property<string>("ReferenceTypeModelUri");
                orn.Property<DateTime?>("ReferenceTypePublicationDate");

                var ornRTFK = orn.HasOne(nr => nr.ReferenceType).WithMany()
                    .HasForeignKey("ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate");

                if (cascadeDelete)
                {
                    ornRTFK.OnDelete(DeleteBehavior.Cascade);
                }
            }
            {
                var orn = modelBuilder.Entity<NodeModel>()
                    .OwnsMany(nm => nm.OtherReferencingNodes);

                orn.WithOwner()
                    .HasForeignKey("OwnerNodeId", "OwnerModelUri", "OwnerPublicationDate");

                orn.Property<string>("ReferencingNodeId");
                orn.Property<string>("ReferencingModelUri");
                orn.Property<DateTime?>("ReferencingPublicationDate");

                var ornFK = orn.HasOne(nr => nr.Node).WithMany()
                    .HasForeignKey("ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate");

                if (cascadeDelete)
                {
                    ornFK.OnDelete(DeleteBehavior.Cascade);
                }

                orn.Property<string>("OwnerNodeId");
                orn.Property<string>("OwnerModelUri");
                orn.Property<DateTime?>("OwnerPublicationDate");

                orn.Property<string>("ReferenceTypeNodeId");
                orn.Property<string>("ReferenceTypeModelUri");
                orn.Property<DateTime?>("ReferenceTypePublicationDate");

                var ornRTFK = orn.HasOne(nr => nr.ReferenceType).WithMany()
                    .HasForeignKey("ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate");

                if (cascadeDelete)
                {
                    ornRTFK.OnDelete(DeleteBehavior.Cascade);
                }
            }
        }

        private void DeclareNodeSetCollection<TEntity>(ModelBuilder modelBuilder, Expression<Func<NodeSetModel, IEnumerable<TEntity>>> collection, bool cascadeDelete) where TEntity : NodeModel
        {
            var collectionName = (collection.Body as MemberExpression).Member.Name;
            var modelProp = $"NodeSet{collectionName}ModelUri";
            var pubDateProp = $"NodeSet{collectionName}PublicationDate";
            modelBuilder.Entity<TEntity>().Property<string>(modelProp);
            modelBuilder.Entity<TEntity>().Property<DateTime?>(pubDateProp);
            var propFK = modelBuilder.Entity<TEntity>().HasOne("Opc.Ua.Cloud.Library.NodeSetModel", null)
                .WithMany(collectionName)
                .HasForeignKey(modelProp, pubDateProp);

            if (cascadeDelete)
            {
                propFK.OnDelete(DeleteBehavior.Cascade);
            }
        }
    }
}
