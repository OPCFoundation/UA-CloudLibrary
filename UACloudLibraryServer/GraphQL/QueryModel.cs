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
    using System.Linq;
    using CESMII.OpcUa.NodeSetModel;
    using HotChocolate;
    using HotChocolate.Authorization;
    using HotChocolate.Data;
    using HotChocolate.Data.Filters;
    using HotChocolate.Language;
    using HotChocolate.Resolvers;
    using HotChocolate.Types;
    using Opc.Ua.Cloud.Library.DbContextModels;

    [Authorize]
    public partial class QueryModel
    {
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<CloudLibNodeSetModel> GetNodeSets([Service(ServiceKind.Synchronized)] IDatabase dp, IResolverContext context,
            string identifier = null, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string[] keywords = null)
        {
            var query = dp.GetNodeSets(identifier, modelUri ?? nodeSetUrl, publicationDate, keywords);

            // Make sure the result is ordered even if the graphl query didn't specify an order so that pagination works correctly
            var orderByArgument = context.ArgumentLiteral<IValueNode>("order");
            if (orderByArgument == NullValueNode.Default || orderByArgument == null)
            {
                query = query.OrderBy(nsm => nsm.ModelUri).ThenBy(nsm => nsm.PublicationDate);
            }

            return query;
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectTypeModel> GetObjectTypes([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetObjectTypes(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<VariableTypeModel> GetVariableTypes([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetVariableTypes(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataTypeModel> GetDataTypes([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetDataTypes(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<PropertyModel> GetProperties([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime ? publicationDate = null, string nodeId = null)
        {
            return dp.GetProperties(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataVariableModel> GetDataVariables([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetDataVariables(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ReferenceTypeModel> GetReferenceTypes([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetReferenceTypes(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<InterfaceModel> GetInterfaces([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetInterfaces(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectModel> GetObjects([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetObjects(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<NodeModel> GetAllNodes([Service(ServiceKind.Synchronized)] IDatabase dp, string modelUri = null, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetAllNodes(modelUri ?? nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<CategoryModel> GetCategories([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetCategories();
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<OrganisationModel> GetOrganisations([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetOrganisations();
        }

#if !NOLEGACY
        #region legacy

        [UsePaging, UseFiltering, UseSorting]
        [Obsolete("Use NodeSets.Metadata instead.")]
        public Task<List<Models.UANameSpace>> GetNamespaces([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            // TODO Return IQueryable to make GraphQL filtering and pagination more efficient.
            return dp.GetNamespaces(short.MaxValue, 0, null, null);
        }


        public class NodeSetGraphQLLegacy
        {
            public string NodesetXml { get; set; }

            public uint Identifier { get; set; }

            public string NamespaceUri { get; set; }

            public string Version { get; set; }

            public DateTime PublicationDate { get; set; }

            public DateTime LastModifiedDate { get; set; }
        }

        [Obsolete("Use namespaces instead.")]
        public Task<List<Models.UANameSpace>> GetNameSpace([Service(ServiceKind.Synchronized)] IDatabase dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetNamespaces(limit, offset, where, orderBy);
        }

        [Obsolete("Use categories instead.")]
        public Task<List<Models.Category>> GetCategory([Service(ServiceKind.Synchronized)] IDatabase dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetCategory(limit, offset, where, orderBy);
        }

        [Obsolete("Use namespaces and namespaces.additionalProperties instead.")]
        public IQueryable<MetadataModel> GetMetadata([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetMetadataModel();
        }

        [Obsolete("Use organizations instead.")]
        public Task<List<Models.Organisation>> GetOrganisation([Service(ServiceKind.Synchronized)] IDatabase dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetOrganisation(limit, offset, where, orderBy);
        }

        [Obsolete("Use nodeSets instead.")]
        public IQueryable<NodeSetGraphQLLegacy> GetNodeSet([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetNodeSet();
        }

        [Obsolete("Use objectTypes instead.")]
        public IQueryable<ObjecttypeModel> GetObjectType([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetObjectType();
        }

        [Obsolete("Use dataTypes instead.")]
        public IQueryable<DatatypeModel> GetDataType([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetDataType();
        }

        [Obsolete("Use referenceTypes instead.")]
        public IQueryable<ReferencetypeModel> GetReferenceType([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetReferenceType();
        }

        [Obsolete("Use variableTypes instead.")]
        public IQueryable<VariabletypeModel> GetVariableType([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return dp.GetVariableType();
        }

        #endregion
#endif // NOLEGACY
    }

    // Turn on paging for all sub-collections
    public class CloudLibNodeSetModelType : ObjectType<CloudLibNodeSetModel>
    {
        protected override void Configure(IObjectTypeDescriptor<CloudLibNodeSetModel> descriptor)
        {
            ConfigureField(descriptor.Field(f => f.ObjectTypes));
            ConfigureField(descriptor.Field(f => f.VariableTypes));
            ConfigureField(descriptor.Field(f => f.DataTypes));
            ConfigureField(descriptor.Field(f => f.Interfaces));
            ConfigureField(descriptor.Field(f => f.Objects));
            ConfigureField(descriptor.Field(f => f.Properties));
            ConfigureField(descriptor.Field(f => f.DataVariables));
            ConfigureField(descriptor.Field(f => f.ReferenceTypes));
            ConfigureField(descriptor.Field(f => f.UnknownNodes));

#if !DEBUG
            descriptor.Field(f => f.ValidationFinishedTime).Ignore();
            descriptor.Field(f => f.ValidationElapsedTime).Ignore();
#endif
        }

        private void ConfigureField(IObjectFieldDescriptor objectFieldDescriptor)
        {
            objectFieldDescriptor
                .UsePaging(options: new HotChocolate.Types.Pagination.PagingOptions { IncludeTotalCount = true })
                .UseFiltering()
                .UseSorting();
        }
    }

    // Hotchocolate 13.x no longer automatically generates filters for custom types (https://github.com/ChilliCream/graphql-platform/pull/5759)
    public class UnsignedIntOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedIntType>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Name("UnsignedIntOperationFilterInputType");
            base.Configure(descriptor);
        }
    }

    public class UnsignedShortOperationFilterInputType
    : ComparableOperationFilterInputType<UnsignedShortType>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Name("UnsignedShortOperationFilterInputType");
            base.Configure(descriptor);
        }
    }
}
