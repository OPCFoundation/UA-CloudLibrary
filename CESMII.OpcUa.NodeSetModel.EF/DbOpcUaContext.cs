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
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Export;

namespace CESMII.OpcUa.NodeSetModel.EF
{
    public class DbOpcUaContext : DefaultOpcUaContext
    {
        protected DbContext _dbContext;
        protected Func<ModelTableEntry, NodeSetModel> _nodeSetFactory;
        protected List<(string ModelUri, DateTime? PublicationDate)> _namespacesInDb;

        public DbOpcUaContext(DbContext appDbContext, ILogger logger, Func<ModelTableEntry, NodeSetModel> nodeSetFactory = null)
            : base(logger)
        {
            this._dbContext = appDbContext;
            this._nodeSetFactory = nodeSetFactory;
            // Get all namespaces with at least one node: used for avoiding DB lookups
            this._namespacesInDb = _dbContext.Set<NodeModel>().Select(nm => new { nm.NodeSet.ModelUri, nm.NodeSet.PublicationDate }).Distinct().AsEnumerable().Select(n => (n.ModelUri, n.PublicationDate)).ToList();
        }
        public DbOpcUaContext(DbContext appDbContext, SystemContext systemContext, NodeStateCollection importedNodes, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger, Func<ModelTableEntry, NodeSetModel> nodeSetFactory = null)
            : base(systemContext, importedNodes, nodesetModels, logger)
        {
            this._dbContext = appDbContext;
            this._nodeSetFactory = nodeSetFactory;
        }

        public override TNodeModel GetModelForNode<TNodeModel>(string nodeId)
        {
            var model = base.GetModelForNode<TNodeModel>(nodeId);
            if (model != null) return model;

            var uaNamespace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);
            NodeModel nodeModelDb;
            if (_nodesetModels.TryGetValue(uaNamespace, out var nodeSet))
            {
                if (!_namespacesInDb.Contains((nodeSet.ModelUri, nodeSet.PublicationDate)))
                {
                    // namespace was not in DB when the context was created: assume it's being imported
                    return null;
                }
                else
                {
                    // With EF9 it is no longer possible to create proxy entities with shadow properties
                    // TODO Find a way to optimize loading the most commonly used nodes to minimize database roundtrips
                    // - Preload
                    // - Find way to make proxies work for entities with shadow properties
                    // Preexisting namespace: find an entity if already in the database
                    int retryCount = 0;
                    bool lookedUp = false;
                    do
                    {
                        try
                        {
                            nodeModelDb = _dbContext.Set<NodeModel>().FirstOrDefault(nm => nm.NodeId == nodeId && nm.NodeSet.ModelUri == nodeSet.ModelUri && nm.NodeSet.PublicationDate == nodeSet.PublicationDate);
                            lookedUp = true;
                        }
                        catch (InvalidOperationException)
                        {
                            // re-try in case the NodeSet access caused a database query that modified the local cache
                            nodeModelDb = null;
                        }
                        retryCount++;
                    } while (!lookedUp && retryCount < 10);
                }
                nodeModelDb?.NodeSet.AllNodesByNodeId.Add(nodeModelDb.NodeId, nodeModelDb);
            }
            else
            {
                nodeModelDb = _dbContext.Set<NodeModel>().FirstOrDefault(nm => nm.NodeId == nodeId && nm.NodeSet.ModelUri == uaNamespace);
                if (nodeModelDb != null)
                {
                    nodeSet = GetOrAddNodesetModel(new ModelTableEntry { ModelUri = nodeModelDb.NodeSet.ModelUri, PublicationDate = nodeModelDb.NodeSet.PublicationDate ?? DateTime.MinValue, PublicationDateSpecified = nodeModelDb.NodeSet.PublicationDate != null });
                    nodeModelDb?.NodeSet.AllNodesByNodeId.Add(nodeModelDb.NodeId, nodeModelDb);
                }
            }
            if (!(nodeModelDb is TNodeModel))
            {
                _logger.LogWarning($"Nodemodel {nodeModelDb} is of type {nodeModelDb.GetType()} when type {typeof(TNodeModel)} was requested. Returning null.");
            }
            return nodeModelDb as TNodeModel;
        }

        public override NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true)
        {
            if (!_nodesetModels.TryGetValue(model.ModelUri, out var nodesetModel))
            {
                var existingNodeSet = GetMatchingOrHigherNodeSetAsync(model.ModelUri, model.GetNormalizedPublicationDate(), model.Version).Result;
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                    nodesetModel = existingNodeSet;
                }
            }
            if (nodesetModel == null && createNew)
            {
                if (_nodeSetFactory == null)
                {
                    nodesetModel = base.GetOrAddNodesetModel(model, createNew);
                    if (nodesetModel.PublicationDate == null)
                    {
                        // Primary Key value can not be null
                        nodesetModel.PublicationDate = DateTime.MinValue;
                    }
                }
                else
                {
                    nodesetModel = _nodeSetFactory.Invoke(model);
                    if (nodesetModel != null)
                    {
                        if (nodesetModel.ModelUri != model.ModelUri)
                        {
                            throw new ArgumentException($"Created mismatching nodeset: expected {model.ModelUri} created {nodesetModel.ModelUri}");
                        }
                        _nodesetModels.Add(nodesetModel.ModelUri, nodesetModel);
                    }
                }
            }
            return nodesetModel;
        }

        public Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(string modelUri, DateTime? publicationDate, string version)
        {
            return GetMatchingOrHigherNodeSetAsync(_dbContext, modelUri, publicationDate, version);
        }
        public static async Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(DbContext dbContext, string modelUri, DateTime? publicationDate, string version)
        {
            var matchingNodeSets = await dbContext.Set<NodeSetModel>()
                .Where(nsm => nsm.ModelUri == modelUri).ToListAsync();
            return NodeSetVersionUtils.GetMatchingOrHigherNodeSet(matchingNodeSets, publicationDate, version);
        }
    }
}
