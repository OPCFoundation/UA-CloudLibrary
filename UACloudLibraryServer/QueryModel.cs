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
using System.Linq.Expressions;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using Extensions;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using Opc.Ua.Cloud.Library.DbContextModels;

namespace Opc.Ua.Cloud.Library
{
    [Authorize]
    public class QueryModel
    {
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<CloudLibNodeSetModel> GetNodeSets([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string identifier = null, string nodeSetUrl = null, DateTime? publicationDate = null)
        {
            IQueryable<CloudLibNodeSetModel> nodeSets;
            if (!string.IsNullOrEmpty(identifier))
            {
                if (nodeSetUrl != null || publicationDate != null)
                {
                    throw new ArgumentException($"Must not specify other parameters when providing identifier.");
                }
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.Identifier == identifier);
            }
            else if (nodeSetUrl != null && publicationDate != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
            }
            else
            {
                nodeSets = dbContext.nodeSets.AsQueryable();
            }
            return nodeSets;
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectTypeModel> GetObjectTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectTypeModel>(dbContext, nsm => nsm.ObjectTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<VariableTypeModel> GetVariableTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<VariableTypeModel>(dbContext, nsm => nsm.VariableTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataTypeModel> GetDataTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataTypeModel>(dbContext, nsm => nsm.DataTypes, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<PropertyModel> GetProperties([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<PropertyModel>(dbContext, nsm => nsm.Properties, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<DataVariableModel> GetDataVariables([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataVariableModel>(dbContext, nsm => nsm.DataVariables, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ReferenceTypeModel> GetReferenceTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ReferenceTypeModel>(dbContext, nsm => nsm.ReferenceTypes, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<InterfaceModel> GetInterfaces([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<InterfaceModel>(dbContext, nsm => nsm.Interfaces, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<ObjectModel> GetObjects([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectModel>(dbContext, nsm => nsm.Objects, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<NodeModel> GetAllNodes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeModel> nodeModels;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeModels = dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl && nm.NodeSet.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeModels = dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl);
            }
            else
            {
                nodeModels = dbContext.nodeModels.AsQueryable();
            }
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        private IQueryable<T> GetNodeModels<T>(AppDbContext dbContext, Expression<Func<CloudLibNodeSetModel, IEnumerable<T>>> selector, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
            where T : NodeModel
        {
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<CloudLibNodeSetModel> nodeSets;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
            }
            else
            {
                nodeSets = dbContext.nodeSets.AsQueryable();
            }
            IQueryable<T> nodeModels = nodeSets.SelectMany(selector);
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }
            return nodeModels;
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNamespaces([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver)
        {
            // TODO run as DB query
            return clResolver.GetNameSpaceTypes(short.MaxValue, 0, null, null);
        }
        [Obsolete("Use namespaces instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNameSpace([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver, int limit, int offset, string where, string orderBy)
        {
            return clResolver.GetNameSpaceTypes(limit, offset, where, orderBy);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategories([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver)
        {
            // TODO run as DB query
            return clResolver.GetCategoryTypes(short.MaxValue, null, null);
        }

        [Obsolete("Use categories instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategory([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver, int limit, int offset, string where, string orderBy)
        {
            return clResolver.GetCategoryTypes(limit, where, orderBy);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<MetadataModel>> GetMetaData([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver)
        {
            return clResolver.GetMetaData();
        }
        [Obsolete("Use metaData instead.")]
        public Task<List<MetadataModel>> GetMetadata([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver)
        {
            return clResolver.GetMetaData();
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Models.Organisation>> GetOrganisations([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver)
        {
            // TODO run as DB query
            return clResolver.GetOrganisationTypes(short.MaxValue, null, null);
        }

        [Obsolete("Use organizations instead.")]
        public Task<List<Models.Organisation>> GetOrganisation([Service(ServiceKind.Synchronized)] UaCloudLibResolver clResolver, int limit, int offset, string where, string orderBy)
        {
            return clResolver.GetOrganisationTypes(limit, where, orderBy);
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
        public IQueryable<NodeSetGraphQLLegacy> GetNodeSet([Service(ServiceKind.Synchronized)] AppDbContext dbContext)
        {
            return dbContext.nodeSets.AsQueryable().Select(nsm => new NodeSetGraphQLLegacy {
                Identifier = uint.Parse(nsm.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = nsm.ModelUri,
                Version = nsm.Version,
                PublicationDate = nsm.PublicationDate ?? default,
                LastModifiedDate = default, // TODO
            });
        }

        [Obsolete("Use objectTypes instead.")]
        public IQueryable<ObjecttypeModel> GetObjectType([Service(ServiceKind.Synchronized)] AppDbContext dbContext)
        {
            var objectTypes = GetNodeModels<ObjectTypeModel>(dbContext, nsm => nsm.ObjectTypes).Select(ot => new ObjecttypeModel {
                BrowseName = ot.BrowseName,
                NameSpace = ot.Namespace,
                NodesetId = long.Parse(ot.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = ot.NodeId.GetDeterministicHashCode(),
                Value = ot.DisplayName.FirstOrDefault().Text,
            });
            return objectTypes;
        }

        [Obsolete("Use dataTypes instead.")]
        public IQueryable<DatatypeModel> GetDataType([Service(ServiceKind.Synchronized)] AppDbContext dbContext)
        {
            var dataTypes = GetNodeModels<DataTypeModel>(dbContext, nsm => nsm.DataTypes).Select(dt => new DatatypeModel {
                BrowseName = dt.BrowseName,
                NameSpace = dt.Namespace,
                NodesetId = long.Parse(dt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = dt.NodeId.GetDeterministicHashCode(),
                Value = dt.DisplayName.FirstOrDefault().Text,
            });
            return dataTypes;
        }
        [Obsolete("Use referenceTypes instead.")]
        public IQueryable<ReferencetypeModel> GetReferenceType([Service(ServiceKind.Synchronized)] AppDbContext dbContext)
        {
            var referenceTypes = GetNodeModels<ReferenceTypeModel>(dbContext, nsm => nsm.ReferenceTypes).Select(rt => new ReferencetypeModel {
                BrowseName = rt.BrowseName,
                NameSpace = rt.Namespace,
                NodesetId = long.Parse(rt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = rt.NodeId.GetDeterministicHashCode(),
                Value = rt.DisplayName.FirstOrDefault().Text,
            });
            return referenceTypes;
        }
        [Obsolete("Use variableTypes instead.")]
        public IQueryable<VariabletypeModel> GetVariableType([Service(ServiceKind.Synchronized)] AppDbContext dbContext)
        {
            var referenceTypes = GetNodeModels<VariableTypeModel>(dbContext, nsm => nsm.VariableTypes).Select(vt => new VariabletypeModel {
                BrowseName = vt.BrowseName,
                NameSpace = vt.Namespace,
                NodesetId = long.Parse(vt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = vt.NodeId.GetDeterministicHashCode(),
                Value = vt.DisplayName.FirstOrDefault().Text,
            });
            return referenceTypes;
        }
        #endregion


#if DEBUG
        [UsePaging, UseFiltering, UseSorting]
        public Opc.Ua.Cloud.Library.Models.UANodesetResult[] GetNodeSetInfo([Service(ServiceKind.Synchronized)] IDatabase database, [Service(ServiceKind.Synchronized)] NodeSetModelIndexerFactory _nodeSetIndexerFactory, string[] keywords)
        {
            var results = database.FindNodesets(keywords ?? new[] { "*" });
            return results;
        }
#endif
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
}
