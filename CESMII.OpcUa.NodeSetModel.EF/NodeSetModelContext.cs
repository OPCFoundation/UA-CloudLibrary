using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CESMII.OpcUa.NodeSetModel.EF
{
    public class NodeSetModelContext : DbContext
    {
        protected bool CascadeDelete { get; set; }
        public NodeSetModelContext(DbContextOptions<NodeSetModelContext> options) : base(options)
        {
            // Blank
        }

        protected NodeSetModelContext(DbContextOptions options)
        {
            // Blank
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseLazyLoadingProxies()
            ;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            CreateModel(modelBuilder, CascadeDelete);

        }
        public static void CreateModel(ModelBuilder modelBuilder, bool cascadeDelete = false, bool methodArgs = false)
        {
            modelBuilder.Owned<NodeModel.LocalizedText>();
            modelBuilder.Owned<NodeModel.NodeAndReference>();
            modelBuilder.Owned<VariableModel.EngineeringUnitInfo>();
            modelBuilder.Owned<DataTypeModel.StructureField>();
            modelBuilder.Owned<DataTypeModel.UaEnumField>();
            modelBuilder.Owned<RequiredModelInfo>();

            modelBuilder.Entity<NodeSetModel>()
                .ToTable("NodeSets")
                .Ignore(nsm => nsm.AllNodesByNodeId)
                .Ignore(nsm => nsm.CustomState)
                .Ignore(nm => nm.NamespaceIndex)
                .HasKey(nsm => new { nsm.ModelUri, nsm.PublicationDate })
                ;
            var rmb = modelBuilder.Entity<NodeSetModel>()
                .OwnsMany(nsm => nsm.RequiredModels)
                ;
            rmb.WithOwner()
                .HasForeignKey("DependentModelUri", "DependentPublicationDate");
            if (cascadeDelete)
            {
                rmb.HasOne(rm => rm.AvailableModel).WithMany()
                    .OnDelete(DeleteBehavior.SetNull);
            }
            modelBuilder.Entity<NodeModel>()
                .Ignore(nm => nm.CustomState)
                .Ignore(nm => nm.ReferencesNotResolved)
                .Property<DateTime?>("NodeSetPublicationDate") // EF tooling does not properly infer the type of this auto-generated property when using it in a foreign key: workaround declare explcitly
                ;
            modelBuilder.Entity<NodeModel>()
                .ToTable("Nodes")
                // This syntax is not supported by EF: use without typing
                //.HasKey(nm => new { nm.NodeId, nm.NodeSet.ModelUri, nm.NodeSet.PublicationDate })
                .HasKey(
                    nameof(NodeModel.NodeId),
                    $"{nameof(NodeModel.NodeSet)}{nameof(NodeSetModel.ModelUri)}",// Foreign key with auto-generated PK of the NodeModel.NodeSet property
                    $"{nameof(NodeModel.NodeSet)}{nameof(NodeSetModel.PublicationDate)}")
                ;
            modelBuilder.Entity<ObjectTypeModel>()
                .ToTable("ObjectTypes")
                ;
            var dtm = modelBuilder.Entity<DataTypeModel>()
                .ToTable("DataTypes");
            if (cascadeDelete)
            {
                dtm.OwnsMany(dt => dt.StructureFields)
                    .HasOne(sf => sf.DataType).WithMany().OnDelete(DeleteBehavior.Cascade)
                ;
            }
            var vtm = modelBuilder.Entity<VariableTypeModel>()
                .ToTable("VariableTypes")
                ;
            if (cascadeDelete)
            {
                vtm.HasOne(vt => vt.DataType).WithMany().OnDelete(DeleteBehavior.Cascade);
            }
            var dvmParentFk = modelBuilder.Entity<DataVariableModel>()
                .ToTable("DataVariables")
                .HasOne(dv => dv.Parent).WithMany()
                    .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate")
                ;
            if (cascadeDelete)
            { 
                    dvmParentFk.OnDelete(DeleteBehavior.Cascade);
            }
            
            var pmParentFk = modelBuilder.Entity<PropertyModel>()
                .ToTable("Properties")
                .HasOne(dv => dv.Parent).WithMany()
                    .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate")
                ;
            if (cascadeDelete)
            {
                pmParentFk.OnDelete(DeleteBehavior.Cascade);
            }
            var omTd = modelBuilder.Entity<ObjectModel>()
                .ToTable("Objects")
                .HasOne<ObjectTypeModel>(o => o.TypeDefinition).WithMany()
                ;
            if (cascadeDelete)
            {
                omTd.OnDelete(DeleteBehavior.Cascade);
            }
            var omParentFk = modelBuilder.Entity<ObjectModel>()
                .HasOne(dv => dv.Parent).WithMany()
                    .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate")
                ;
            if (cascadeDelete)
            {
                omParentFk.OnDelete(DeleteBehavior.Cascade);
            }
            modelBuilder.Entity<InterfaceModel>()
                .ToTable("Interfaces")
                ;
            modelBuilder.Entity<VariableModel>()
                .ToTable("Variables")
                .OwnsOne(v => v.EngineeringUnit).Property(v => v.NamespaceUri).IsRequired()
                ;
            if (cascadeDelete)
            {
                modelBuilder.Entity<VariableModel>()
                    .HasOne(vm => vm.DataType).WithMany().OnDelete(DeleteBehavior.Cascade);
                modelBuilder.Entity<VariableModel>()
                    .HasOne(vm => vm.TypeDefinition).WithMany().OnDelete(DeleteBehavior.Cascade);
            }
            var btmSt = modelBuilder.Entity<BaseTypeModel>()
                .ToTable("BaseTypes")
                .HasOne(bt => bt.SuperType).WithMany(bt => bt.SubTypes)
                ;
            if (cascadeDelete)
            {
                btmSt.OnDelete(DeleteBehavior.Cascade);
            }
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
                    .HasForeignKey("ParentNodeId", "ParentModelUri", "ParentPublicationDate")
                ;
            if (cascadeDelete)
            {
                mmParentFk.OnDelete(DeleteBehavior.Cascade);
            }
            if (cascadeDelete)
            {
                modelBuilder.Entity<MethodModel>()
                    .HasOne(mm => mm.TypeDefinition).WithMany().OnDelete(DeleteBehavior.Cascade);
            }
            modelBuilder.Entity<ReferenceTypeModel>()
                .ToTable("ReferenceTypes")
                ;

            #region NodeSetModel collections
            DeclareNodeSetCollection<ObjectTypeModel>(modelBuilder, nsm => nsm.ObjectTypes, cascadeDelete);
            DeclareNodeSetCollection<VariableTypeModel>(modelBuilder, nsm => nsm.VariableTypes, cascadeDelete);
            DeclareNodeSetCollection<DataTypeModel>(modelBuilder, nsm => nsm.DataTypes, cascadeDelete);
            DeclareNodeSetCollection<ReferenceTypeModel>(modelBuilder, nsm => nsm.ReferenceTypes, cascadeDelete);
            DeclareNodeSetCollection<ObjectModel>(modelBuilder, nsm => nsm.Objects, cascadeDelete);
            DeclareNodeSetCollection<MethodModel>(modelBuilder, nsm => nsm.Methods, cascadeDelete);
            DeclareNodeSetCollection<InterfaceModel>(modelBuilder, nsm => nsm.Interfaces, cascadeDelete);
            DeclareNodeSetCollection<PropertyModel>(modelBuilder, nsm => nsm.Properties, cascadeDelete);
            DeclareNodeSetCollection<DataVariableModel>(modelBuilder, nsm => nsm.DataVariables, cascadeDelete);
            DeclareNodeSetCollection<NodeModel>(modelBuilder, nsm => nsm.UnknownNodes, cascadeDelete);
            #endregion

            #region NodeModel collections
            // Unclear why these collection require declarations while the others just work
            modelBuilder.Entity<DataVariableModel>()
                .HasMany(dv => dv.NodesWithDataVariables).WithMany(nm => nm.DataVariables);
            modelBuilder.Entity<NodeModel>()
                .HasMany(nm => nm.Properties).WithMany(v => v.NodesWithProperties);
            modelBuilder.Entity<NodeModel>()
                .HasMany(nm => nm.Interfaces).WithMany(v => v.NodesWithInterface);

            #endregion

            {
                var orn = modelBuilder.Entity<NodeModel>()
                    .OwnsMany<NodeModel.NodeAndReference>(nm => nm.OtherReferencedNodes)
                    ;
                orn.WithOwner()
                    .HasForeignKey("OwnerNodeId", "OwnerModelUri", "OwnerPublicationDate")
                    ;
                orn.Property<string>("ReferencedNodeId");
                orn.Property<string>("ReferencedModelUri");
                orn.Property<DateTime?>("ReferencedPublicationDate");
                var ornFK = orn.HasOne(nr => nr.Node).WithMany()
                    .HasForeignKey("ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate")
                ;
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
                //orn.Property(nr => nr.ReferenceType)
                //    .HasConversion<NodeModel>()
                //    //.HasColumnType<ReferenceTypeModel>(typeof(NodeModel).FullName)
                //    //.HasColumnType<NodeModel>(typeof(NodeModel).FullName)
                //    ;
                var ornRTFK = orn.HasOne(nr => nr.ReferenceType).WithMany()
                    .HasForeignKey("ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate")
                    //.HasPrincipalKey("NodeId", "ModelUri", "PublicationDate")
                    ;
                if (cascadeDelete)
                {
                    ornRTFK.OnDelete(DeleteBehavior.Cascade);
                }
            }
            {
                var orn = modelBuilder.Entity<NodeModel>()
                    .OwnsMany<NodeModel.NodeAndReference>(nm => nm.OtherReferencingNodes)
                    ;
                orn.WithOwner()
                    .HasForeignKey("OwnerNodeId", "OwnerModelUri", "OwnerPublicationDate")
                    ;
                orn.Property<string>("ReferencingNodeId");
                orn.Property<string>("ReferencingModelUri");
                orn.Property<DateTime?>("ReferencingPublicationDate");
                var ornFK = orn.HasOne(nr => nr.Node).WithMany()
                    .HasForeignKey("ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate")
                ;
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
                // TODO figure out why this does not work if ReferenceType is declared as ReferenceTypeModel instead of NodeModel
                //orn.Property(nr => nr.ReferenceType)
                //    .HasConversion<NodeModel>()
                //    //.HasColumnType<ReferenceTypeModel>(typeof(NodeModel).FullName)
                //    //.HasColumnType<NodeModel>(typeof(NodeModel).FullName)
                //    ;
                var ornRTFK = orn.HasOne(nr => nr.ReferenceType).WithMany()
                    .HasForeignKey("ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate")
                    ;
                if (cascadeDelete)
                {
                    ornRTFK.OnDelete(DeleteBehavior.Cascade);
                }

            }
        }

        private static void DeclareNodeSetCollection<TEntity>(ModelBuilder modelBuilder, Expression<Func<NodeSetModel, IEnumerable<TEntity>>> collection, bool cascadeDelete) where TEntity : NodeModel
        {
            var collectionName = (collection.Body as MemberExpression).Member.Name;
            var modelProp = $"NodeSet{collectionName}ModelUri";
            var pubDateProp = $"NodeSet{collectionName}PublicationDate";
            modelBuilder.Entity<TEntity>().Property<string>(modelProp);
            modelBuilder.Entity<TEntity>().Property<DateTime?>(pubDateProp);
            var propFK = modelBuilder.Entity<TEntity>().HasOne("CESMII.OpcUa.NodeSetModel.NodeSetModel", null)
                .WithMany(collectionName)
                .HasForeignKey(modelProp, pubDateProp);
            if (cascadeDelete)
            {
                propFK.OnDelete(DeleteBehavior.Cascade);
            }
            // With this typed declaration the custom property names are not picked up for some reason
            //modelBuilder.Entity<TEntity>()
            //    .HasOne(nm => nm.NodeSet).WithMany(collection)
            //        .HasForeignKey(modelProp, pubDateProp)
            //        ;
            //modelBuilder.Entity<NodeSetModel>()
            //    .HasMany(collection).WithOne(nm => nm.NodeSet)
            //        .HasForeignKey(modelProp, pubDateProp)
            //    ;
        }

        public DbSet<NodeSetModel> NodeSets { get; set; }
        public DbSet<NodeModel> NodeModels { get; set; }
    }

}