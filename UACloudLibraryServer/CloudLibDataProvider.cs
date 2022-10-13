using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using Extensions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibDataProvider
    {
        private readonly AppDbContext _context = null;
        private readonly UaCloudLibResolver _resolver = null;
        private readonly IDatabase _database = null;

        public CloudLibDataProvider(AppDbContext context, IDatabase database)
        {
            _context = context;
            _resolver = new UaCloudLibResolver(context, database);
            _database = database;
        }

        public IQueryable<CloudLibNodeSetModel> GetNodeSets(
            string identifier = null,
            string nodeSetUrl = null,
            DateTime? publicationDate = null,
            string[] keywords = null)
        {

            IQueryable<CloudLibNodeSetModel> nodeSets;
            if (!string.IsNullOrEmpty(identifier))
            {
                if (nodeSetUrl != null || publicationDate != null || keywords != null)
                {
                    throw new ArgumentException($"Must not specify other parameters when providing identifier.");
                }
                nodeSets = this._context.nodeSets.AsQueryable().Where(nsm => nsm.Identifier == identifier);
            }
            else
            {
                var nodeSetQuery = _database.SearchNodesets(keywords);
                if (nodeSetUrl != null && publicationDate != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
                }
                else if (nodeSetUrl != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == nodeSetUrl);
                }
                else
                {
                    nodeSets = nodeSetQuery;
                }
            }
            return nodeSets;
        }

        public IQueryable<NodeModel> GetAllNodes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
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
                nodeModels = _context.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl && nm.NodeSet.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeModels = _context.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl);
            }
            else
            {
                nodeModels = _context.nodeModels.AsQueryable();
            }
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        public IQueryable<T> GetNodeModels<T>(Expression<Func<CloudLibNodeSetModel, IEnumerable<T>>> selector, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
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
                nodeSets = _context.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = _context.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
            }
            else
            {
                nodeSets = _context.nodeSets.AsQueryable();
            }
            IQueryable<T> nodeModels = nodeSets.SelectMany(selector);
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }
            return nodeModels;
        }

        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNamespaces()
        {
            return GetNamespaces(short.MaxValue, 0, null, null);
        }

        public Task<List<Models.UANameSpace>> GetNamespaces(int limit, int offset, string where, string orderBy)
        {
            // TODO run as DB query
            return _resolver.GetNameSpaceTypes(limit, offset, where, orderBy);
        }

        public int GetNamespaceTotalCount(string where)
        {
            return _resolver.GetNameSpaceTypesTotalCount(where);
        }

        [Obsolete("Use namespaces instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.UANameSpace>> GetNameSpace(int limit, int offset, string where, string orderBy)
        {
            return _resolver.GetNameSpaceTypes(limit, offset, where, orderBy);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategories()
        {
            // TODO run as DB query
            return _resolver.GetCategoryTypes(short.MaxValue, null, null);
        }

        [Obsolete("Use categories instead.")]
        public Task<List<Opc.Ua.Cloud.Library.Models.Category>> GetCategory(int limit, int offset, string where, string orderBy)
        {
            return _resolver.GetCategoryTypes(limit, where, orderBy);
        }

        [Obsolete("Use namespaces and namespaces.additionalProperties instead.")]
        public Task<List<MetadataModel>> GetMetadata()
        {
            return _resolver.GetMetaData();
        }
        public UANameSpaceBase GetMetadata(uint nodeSetId)
        {
            return (_database as PostgreSQLDB).RetrieveAllMetadata(nodeSetId);
        }

        [UsePaging, UseFiltering, UseSorting]
        public Task<List<Models.Organisation>> GetOrganisations()
        {
            // TODO run as DB query
            return _resolver.GetOrganisationTypes(short.MaxValue, null, null);
        }

        [Obsolete("Use organizations instead.")]
        public Task<List<Models.Organisation>> GetOrganisation(int limit, int offset, string where, string orderBy)
        {
            return _resolver.GetOrganisationTypes(limit, where, orderBy);
        }

        #region legacy

        [Obsolete("Use nodeSets instead.")]
        public IQueryable<QueryModel.NodeSetGraphQLLegacy> GetNodeSet()
        {
            return _context.nodeSets.AsQueryable().Select(nsm => new QueryModel.NodeSetGraphQLLegacy {
                Identifier = uint.Parse(nsm.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = nsm.ModelUri,
                Version = nsm.Version,
                PublicationDate = nsm.PublicationDate ?? default,
                LastModifiedDate = default, // TODO
            });
        }

        [Obsolete("Use objectTypes instead.")]
        public IQueryable<ObjecttypeModel> GetObjectType()
        {
            var objectTypes = GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes).Select(ot => new ObjecttypeModel {
                BrowseName = ot.BrowseName,
                NameSpace = ot.Namespace,
                NodesetId = long.Parse(ot.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = ot.NodeId.GetDeterministicHashCode(),
                Value = ot.DisplayName.FirstOrDefault().Text,
            });
            return objectTypes;
        }

        [Obsolete("Use dataTypes instead.")]
        public IQueryable<DatatypeModel> GetDataType()
        {
            var dataTypes = GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes).Select(dt => new DatatypeModel {
                BrowseName = dt.BrowseName,
                NameSpace = dt.Namespace,
                NodesetId = long.Parse(dt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = dt.NodeId.GetDeterministicHashCode(),
                Value = dt.DisplayName.FirstOrDefault().Text,
            });
            return dataTypes;
        }
        [Obsolete("Use referenceTypes instead.")]
        public IQueryable<ReferencetypeModel> GetReferenceType()
        {
            var referenceTypes = GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes).Select(rt => new ReferencetypeModel {
                BrowseName = rt.BrowseName,
                NameSpace = rt.Namespace,
                NodesetId = long.Parse(rt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = rt.NodeId.GetDeterministicHashCode(),
                Value = rt.DisplayName.FirstOrDefault().Text,
            });
            return referenceTypes;
        }
        [Obsolete("Use variableTypes instead.")]
        public IQueryable<VariabletypeModel> GetVariableType()
        {
            var referenceTypes = GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes).Select(vt => new VariabletypeModel {
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
        public Opc.Ua.Cloud.Library.Models.UANodesetResult[] GetNodeSetInfo(string[] keywords)
        {
            var results = _database.FindNodesets(keywords ?? new[] { "*" }, 0, 100);
            return results;
        }
#endif
    }
}
