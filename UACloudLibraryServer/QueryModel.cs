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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Data;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Opc.Ua.Cloud.Library.DbContextModels;

namespace Opc.Ua.Cloud.Library
{
    [Authorize]
    public class QueryModel
    {
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<CloudLibNodeSetModel> GetNodeSets([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, IResolverContext context,
            string identifier = null, string nodeSetUrl = null, DateTime? publicationDate = null, string[] keywords = null)
        {
            var query = dp.GetNodeSets(identifier, nodeSetUrl, publicationDate, keywords);
            var orderByArgument = context.ArgumentLiteral<IValueNode>("order");
            if (orderByArgument == NullValueNode.Default || orderByArgument == null)
            {
                query = query.OrderBy(nsm => nsm.ModelUri).ThenBy(nsm => nsm.PublicationDate);
            }

            return query;
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectTypeModel> GetObjectTypes([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<VariableTypeModel> GetVariableTypes([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataTypeModel> GetDataTypes([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<PropertyModel> GetProperties([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<PropertyModel>(nsm => nsm.Properties, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataVariableModel> GetDataVariables([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<DataVariableModel>(nsm => nsm.DataVariables, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ReferenceTypeModel> GetReferenceTypes([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<InterfaceModel> GetInterfaces([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<InterfaceModel>(nsm => nsm.Interfaces, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectModel> GetObjects([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetNodeModels<ObjectModel>(nsm => nsm.Objects, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<NodeModel> GetAllNodes([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return dp.GetAllNodes(nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNamespaces([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            // TODO run as DB query
            return dp.GetNamespaces();
        }
        [Obsolete("Use namespaces instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNameSpace([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetNameSpace(limit, offset, where, orderBy);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategories([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            // TODO run as DB query
            return dp.GetCategories();
        }

        [Obsolete("Use categories instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategory([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetCategory(limit, offset, where, orderBy);
        }

        [Obsolete("Use namespaces and namespaces.additionalProperties instead.")]
        public Task<List<MetadataModel>> GetMetadata([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetMetadata();
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Models.Organisation>> GetOrganisations([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            // TODO run as DB query
            return dp.GetOrganisations();
        }

        [Obsolete("Use organizations instead.")]
        public Task<List<Models.Organisation>> GetOrganisation([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp, int limit, int offset, string where, string orderBy)
        {
            return dp.GetOrganisation(limit, offset, where, orderBy);
        }

        #region legacy

        public class NodeSetGraphQLLegacy
        {
            public string NodesetXml { get; set; }
            public uint Identifier { get; set; }
            public string NamespaceUri { get; set; }
            public string Version { get; set; }
            public DateTime PublicationDate { get; set; }
            public DateTime LastModifiedDate { get; set; }
        }

        [Obsolete("Use nodeSets instead.")]
        public IQueryable<NodeSetGraphQLLegacy> GetNodeSet([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetNodeSet();
        }

        [Obsolete("Use objectTypes instead.")]
        public IQueryable<ObjecttypeModel> GetObjectType([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetObjectType();
        }

        [Obsolete("Use dataTypes instead.")]
        public IQueryable<DatatypeModel> GetDataType([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetDataType();
        }
        [Obsolete("Use referenceTypes instead.")]
        public IQueryable<ReferencetypeModel> GetReferenceType([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetReferenceType();
        }
        [Obsolete("Use variableTypes instead.")]
        public IQueryable<VariabletypeModel> GetVariableType([Service(ServiceKind.Synchronized)] CloudLibDataProvider dp)
        {
            return dp.GetVariableType();
        }
        #endregion
    }

    // Turn on paging for all sub-collections
    public class CloudLibNodeSetModelType : ObjectType<CloudLibNodeSetModel>
    {
        protected override void Configure(IObjectTypeDescriptor<CloudLibNodeSetModel> descriptor)
        {
            descriptor.Field(f => f.Metadata).Resolve(context => {
                var parent = context.Parent<CloudLibNodeSetModel>();
                var metaData = context.Service<CloudLibDataProvider>().GetMetadata(uint.Parse(parent.Identifier, CultureInfo.InvariantCulture));
                return metaData;
            });

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
}
