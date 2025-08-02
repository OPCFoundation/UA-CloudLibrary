using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public class DefaultOpcUaContext
    {
        private readonly SystemContext _systemContext;

        private readonly NodeStateCollection _importedNodes;

        protected Dictionary<string, NodeSetModel> NodeSetModelDictionary { get; set; } = new Dictionary<string, NodeSetModel>();

        private ILogger _logger;

        public DefaultOpcUaContext(ILogger logger)
        {
            _importedNodes = new NodeStateCollection();
            _logger = logger ?? NullLogger.Instance;

            var namespaceTable = new NamespaceTable();
            namespaceTable.GetIndexOrAppend(Namespaces.OpcUa);
            var typeTable = new TypeTable(namespaceTable);
            _systemContext = new SystemContext() {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable
            };
        }

        private Dictionary<NodeId, NodeState> _importedNodesByNodeId;

        private Dictionary<string, UANodeSet> _importedUANodeSetsByUri = new();

        public NamespaceTable NamespaceUris { get => _systemContext.NamespaceUris; }

        public ILogger Logger => _logger;

        public bool UseLocalNodeIds { get; set; }

        public virtual string GetModelNodeId(NodeId nodeId)
        {
            string namespaceUri;
            namespaceUri = GetNamespaceUri(nodeId.NamespaceIndex);
            if (string.IsNullOrEmpty(namespaceUri))
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid, "Namespace Index ({0}) for node id {1} is not in the namespace table.", nodeId.NamespaceIndex, nodeId);
            }
            if (UseLocalNodeIds)
            {
                return nodeId.ToString();
            }
            var nodeIdWithUri = new ExpandedNodeId(nodeId, namespaceUri).ToString();
            return nodeIdWithUri;
        }

        public virtual NodeState GetNode(ExpandedNodeId expandedNodeId)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, _systemContext.NamespaceUris);

            return GetNode(nodeId);
        }

        public virtual NodeState GetNode(NodeId nodeId)
        {
            _importedNodesByNodeId ??= _importedNodes.ToDictionary(n => n.NodeId);
            NodeState nodeStateDict = null;

            if (nodeId != null)
            {
                _importedNodesByNodeId.TryGetValue(nodeId, out nodeStateDict);
            }

            return nodeStateDict;
        }

        public virtual string GetNamespaceUri(ushort namespaceIndex)
        {
            return _systemContext.NamespaceUris.GetString(namespaceIndex);
        }

        public virtual TNodeModel GetModelForNode<TNodeModel>(string nodeId) where TNodeModel : NodeModel
        {
            foreach (var nodeSetModel in NodeSetModelDictionary.Values)
            {
                if (nodeSetModel.AllNodesByNodeId.TryGetValue(nodeId, out var nodeModel))
                {
                    var result = nodeModel as TNodeModel;
                    return result;
                }
            }

            return null;
        }

        public virtual NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true)
        {
            if (!NodeSetModelDictionary.TryGetValue(model.ModelUri, out var nodesetModel))
            {
                nodesetModel = new NodeSetModel();
                nodesetModel.ModelUri = model.ModelUri;
                nodesetModel.PublicationDate = model.GetNormalizedPublicationDate();
                nodesetModel.Version = model.Version;

                if (!string.IsNullOrEmpty(model.XmlSchemaUri))
                {
                    nodesetModel.XmlSchemaUri = model.XmlSchemaUri;
                }

                if (UseLocalNodeIds)
                {
                    nodesetModel.NamespaceIndex = NamespaceUris.GetIndexOrAppend(nodesetModel.ModelUri);
                }

                if (model.RequiredModel != null)
                {
                    foreach (var requiredModel in model.RequiredModel)
                    {
                        var existingNodeSet = GetOrAddNodesetModel(requiredModel);
                        var requiredModelInfo = new RequiredModelInfoModel {
                            ModelUri = requiredModel.ModelUri,
                            PublicationDate = requiredModel.GetNormalizedPublicationDate(),
                            Version = requiredModel.Version,
                            AvailableModel = existingNodeSet,
                        };
                        nodesetModel.RequiredModels.Add(requiredModelInfo);
                    }
                }

                NodeSetModelDictionary.Add(nodesetModel.ModelUri, nodesetModel);
            }

            return nodesetModel;
        }

        public virtual List<NodeState> ImportUANodeSet(UANodeSet nodeSet)
        {
            var previousNodes = _importedNodes.ToList();
            if (nodeSet.Items?.Length > 0)
            {
                nodeSet.Import(_systemContext, _importedNodes);
            }

            var newlyImportedNodes = _importedNodes.Except(previousNodes).ToList();
            if (newlyImportedNodes.Count > 0)
            {
                _importedNodesByNodeId = null;
            }

            var modelUri = nodeSet.Models?.FirstOrDefault()?.ModelUri;
            if (modelUri != null)
            {
                _importedUANodeSetsByUri.Add(modelUri, nodeSet);
            }

            return newlyImportedNodes;
        }

        public virtual UANodeSet GetUANodeSet(string modeluri)
        {
            if (_importedUANodeSetsByUri.TryGetValue(modeluri, out var nodeSet))
            {
                return nodeSet;
            }

            return null;
        }

        public virtual List<NodeStateHierarchyReference> GetHierarchyReferences(NodeState nodeState)
        {
            var hierarchy = new Dictionary<NodeId, string>();
            var references = new List<NodeStateHierarchyReference>();
            nodeState.GetHierarchyReferences(_systemContext, null, hierarchy, references);

            return references;
        }

        public virtual (string Json, bool IsScalar) JsonEncodeVariant(Variant wrappedValue, DataTypeModel dataType = null)
        {
            throw new NotImplementedException();
        }

        public virtual Variant JsonDecodeVariant(string jsonVariant, DataTypeModel dataType = null)
        {
            throw new NotImplementedException();
        }

        public string GetModelBrowseName(QualifiedName browseName)
        {
            if (UseLocalNodeIds)
            {
                return browseName.ToString();
            }

            return $"{NamespaceUris.GetString(browseName.NamespaceIndex)};{browseName.Name}";
        }
    }
}
