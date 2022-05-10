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

using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Cloud.Library
{
    internal class DbOpcUaContext: IOpcUaContext
    {
        private AppDbContext appDbContext;
        private ILogger logger;
        private readonly Dictionary<string, NodeSetModel> _nodesetModels;
        private DefaultOpcUaContext _opcContext;

        public DbOpcUaContext(AppDbContext appDbContext, SystemContext systemContext, NodeStateCollection importedNodes, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger)
        {
            this.appDbContext = appDbContext;
            this.logger = logger;
            this._nodesetModels = nodesetModels;
            this._opcContext = new DefaultOpcUaContext(systemContext, importedNodes, nodesetModels, logger);
        }

        public NodeModel GetModelForNode(string nodeId)
        {
            var model = _opcContext.GetModelForNode(nodeId);
            if (model != null) return model;

            var uaNamespace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);

            var nodeSetQuery = appDbContext.Set<NodeSetModel>().AsQueryable().Where(n => n.ModelUri == uaNamespace);
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

        public NodeSetModel GetOrAddNodesetModel(NodeModel node)
        {
            if (!_nodesetModels.TryGetValue(node.Namespace, out var nodesetModel))
            {
                var existingNodeSet = appDbContext.nodeSets.FirstOrDefault(n => n.ModelUri == node.Namespace);
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                }
            }
            return _opcContext.GetOrAddNodesetModel(node);
        }

        public string JsonEncodeVariant(Variant wrappedValue)
        {
            return (_opcContext as IOpcUaContext).JsonEncodeVariant(wrappedValue);
        }
    }
}
