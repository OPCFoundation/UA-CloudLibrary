/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 *
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    /// <summary>
    /// Main Importer class importing NodeSets
    /// </summary>
    public class NodeSetModelImporter
    {
        private readonly NodeSetImporterManager _nodeSetCacheManager;
        private readonly IOpcUaContext _opcContext;

        public NodeSetModelImporter(IOpcUaContext opcContext, NodeSetImporter nodeSetCache)
        {
            _opcContext = opcContext;
            _nodeSetCacheManager = new NodeSetImporterManager(nodeSetCache);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="nodeSetXML">nodeset to be imported.</param>
        /// <param name="identifier">optional identifier to be attached to the nodeset. For use by the application.</param>
        /// <param name="tenantId">optional identifier to be used by the nodeSetCache to distinguish between tenants in a multi-tenant system</param>
        /// <param name="failOnExistingNodeSet">Fail if the nodeset already exists in the nodeSetCache.</param>
        /// <param name="loadAllDependentModels">Fully load all dependent models. Otherwise, dependent types will only be resolved when referenced by a subsequently imported nodeset.</param>
        /// <returns></returns>
        /// <exception cref="NodeSetResolverException"></exception>
        public async Task<List<NodeSetModel>> ImportNodeSetModelAsync(string nodeSetXML, string identifier = null, object tenantId = null, bool failOnExistingNodeSet = false, bool loadAllDependentModels = false)
        {
            _opcContext.NamespaceUris.GetIndexOrAppend(Namespaces.OpcUa);
            var resolvedNodeSets = _nodeSetCacheManager.ImportNodeSets(new List<string> { nodeSetXML }, failOnExistingNodeSet, tenantId);
            if (!string.IsNullOrEmpty(resolvedNodeSets.ErrorMessage))
            {
                throw new Exception($"{resolvedNodeSets.ErrorMessage}");
            }

            var firstNewNodeset = resolvedNodeSets.Models.FirstOrDefault(m => m.NewInThisImport || m.RequestedForThisImport);
            if (firstNewNodeset?.NodeSet?.NamespaceUris?.Any() == true)
            {
                // Ensure namespaces are in the context and in proper order
                var namespaces = firstNewNodeset.NodeSet.NamespaceUris.ToList();
                if (namespaces[0] != Namespaces.OpcUa)
                {
                    namespaces.Insert(0, Namespaces.OpcUa);
                }
                namespaces.ForEach(n => _opcContext.NamespaceUris.GetIndexOrAppend(n));
                if (!namespaces.Take(_opcContext.NamespaceUris.Count).SequenceEqual(_opcContext.NamespaceUris.ToArray().Take(namespaces.Count)))
                {
                    throw new Exception($"Namespace table for {firstNewNodeset} is not in the order required by the nodeset.");
                }
            }

            List<NodeSetModel> allLoadedNodesetModels = new();

            foreach (var resolvedModel in resolvedNodeSets.Models)
            {
                if (loadAllDependentModels || resolvedModel.RequestedForThisImport || resolvedModel.NewInThisImport)
                {
                    List<NodeSetModel> loadedNodesetModels = await NodeModelFactoryOpc.LoadNodeSetAsync(_opcContext, resolvedModel.NodeSet, null, new Dictionary<string, string>(), true);
                    foreach (var nodeSetModel in loadedNodesetModels)
                    {
                        nodeSetModel.Identifier = identifier;
                        nodeSetModel.HeaderComments = resolvedModel.HeaderComment;
                        if (_opcContext.UseLocalNodeIds)
                        {
                            nodeSetModel.NamespaceIndex = _opcContext.NamespaceUris.GetIndex(nodeSetModel.ModelUri);
                        }
                    }
                    allLoadedNodesetModels.AddRange(loadedNodesetModels);
                }
                else
                {
                    var existingNodeSetModel = _opcContext.GetOrAddNodesetModel(resolvedModel.NodeSet.Models.FirstOrDefault(), false);

                    if (existingNodeSetModel == null)
                    {
                        throw new ArgumentException($"Required NodeSet {existingNodeSetModel} not in database: Inconsistency between file store and db?");
                    }
                    // Get the node state for required models so that UANodeSet.Import works
                    _opcContext.ImportUANodeSet(resolvedModel.NodeSet);
                }
            }

            return allLoadedNodesetModels;
        }
    }
}
