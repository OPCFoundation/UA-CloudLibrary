/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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
using System.Linq;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library
{
    internal class DbOpcUaContext : IOpcUaContext
    {
        private AppDbContext _dbContext;
        private ILogger logger;
        private readonly Dictionary<string, NodeSetModel> _nodesetModels;
        private DefaultOpcUaContext _opcContext;

        public DbOpcUaContext(AppDbContext appDbContext, SystemContext systemContext, NodeStateCollection importedNodes, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger)
        {
            this._dbContext = appDbContext;
            this.logger = logger;
            this._nodesetModels = nodesetModels;
            this._opcContext = new DefaultOpcUaContext(systemContext, importedNodes, nodesetModels, logger);
        }

        public NodeModel GetModelForNode(string nodeId)
        {
            var model = _opcContext.GetModelForNode(nodeId);
            if (model != null) return model;

            var uaNamespace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);

            var nodeSetQuery = _dbContext.Set<NodeSetModel>().AsQueryable().Where(n => n.ModelUri == uaNamespace);
            if (nodeSetQuery.Any())
            {
                // TODO Performance: do this in a single query
                model = nodeSetQuery.SelectMany(m => m.DataTypes).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model as DataTypeModel;
                model = nodeSetQuery.SelectMany(m => m.ObjectTypes).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model as ObjectTypeModel;
                model = nodeSetQuery.SelectMany(m => m.VariableTypes).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model as VariableTypeModel;
                model = nodeSetQuery.SelectMany(m => m.Properties).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model;
                model = nodeSetQuery.SelectMany(m => m.DataVariables).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model;
                model = nodeSetQuery.AsQueryable().SelectMany(m => m.Interfaces).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model;
                model = nodeSetQuery.AsQueryable().SelectMany(m => m.Objects).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model;
                model = nodeSetQuery.AsQueryable().SelectMany(m => m.ReferenceTypes).Where(dt => dt.NodeId == nodeId).FirstOrDefault();
                if (model != null) return model;
            }
            return null;
        }


        public NamespaceTable NamespaceUris => _opcContext.NamespaceUris;

        public ILogger Logger => logger;

        public List<NodeStateHierarchyReference> GetHierarchyReferences(NodeState nodeState)
        {
            return _opcContext.GetHierarchyReferences(nodeState);
        }

        public NodeState GetNode(NodeId referenceTypeId)
        {
            return _opcContext.GetNode(referenceTypeId);
        }

        public NodeState GetNode(ExpandedNodeId expandedNodeId)
        {
            return _opcContext.GetNode(expandedNodeId);
        }

        public string GetNodeIdWithUri(NodeId nodeId, out string namespaceUri)
        {
            return _opcContext.GetNodeIdWithUri(nodeId, out namespaceUri);
        }

        public NodeSetModel GetOrAddNodesetModel(NodeModel nodeModel)
        {
            if (!_nodesetModels.TryGetValue(nodeModel.Namespace, out var nodesetModel))
            {
                var existingNodeSet = GetMatchingOrHigherNodeSetAsync(nodeModel.Namespace, nodeModel.NodeSet?.PublicationDate).Result;
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                    nodesetModel = existingNodeSet;
                }
            }
            if (nodesetModel == null)
            {
                throw new System.Exception($"Undeclared nodeset model {nodeModel.Namespace} was referenced");
            }
            nodeModel.NodeSet = nodesetModel;
            return nodesetModel;
        }

        public Task<CloudLibNodeSetModel> GetMatchingOrHigherNodeSetAsync(string modelUri, DateTime? publicationDate)
        {
            return GetMatchingOrHigherNodeSetAsync(_dbContext, modelUri, publicationDate);
        }
        public static Task<CloudLibNodeSetModel> GetMatchingOrHigherNodeSetAsync(AppDbContext dbContext, string modelUri, DateTime? publicationDate)
        {
            var matchingNodeSet = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri && (publicationDate == null || nsm.PublicationDate >= publicationDate)).OrderBy(nsm => nsm.PublicationDate).FirstOrDefaultAsync();
            return matchingNodeSet;
        }

        public string JsonEncodeVariant(Variant wrappedValue)
        {
            return (_opcContext as IOpcUaContext).JsonEncodeVariant(wrappedValue);
        }
    }
}
