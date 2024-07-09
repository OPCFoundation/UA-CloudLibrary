using Opc.Ua;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Opc.Ua.Export;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using CESMII.OpcUa.NodeSetModel.Export.Opc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CESMII.OpcUa.NodeSetModel.Factory.Opc
{
    public class DefaultOpcUaContext : IOpcUaContext
    {
        private readonly ISystemContext _systemContext;
        private readonly NodeStateCollection _importedNodes;
        protected readonly Dictionary<string, NodeSetModel> _nodesetModels;
        protected readonly ILogger _logger;

        public DefaultOpcUaContext(ILogger logger)
        {
            _importedNodes = new NodeStateCollection();
            _nodesetModels = new Dictionary<string, NodeSetModel>();
            _logger = logger ?? NullLogger.Instance;

            var namespaceTable = new NamespaceTable();
            namespaceTable.GetIndexOrAppend(Namespaces.OpcUa);
            var typeTable = new TypeTable(namespaceTable);
            _systemContext = new SystemContext()
            {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable,
                EncodeableFactory = new DynamicEncodeableFactory(EncodeableFactory.GlobalFactory),
            };
        }

        public DefaultOpcUaContext(Dictionary<string, NodeSetModel> nodesetModels, ILogger logger) : this(logger)
        {
            _nodesetModels = nodesetModels;
            _logger = logger ?? NullLogger.Instance;
        }
        public DefaultOpcUaContext(ISystemContext systemContext, NodeStateCollection importedNodes, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger)
            : this(nodesetModels, logger)
        {
            _systemContext = systemContext;
            _importedNodes = importedNodes;
        }

        public bool ReencodeExtensionsAsJson { get; set; }
        public bool EncodeJsonScalarsAsValue { get; set; }

        private Dictionary<NodeId, NodeState> _importedNodesByNodeId;
        private Dictionary<string, UANodeSet> _importedUANodeSetsByUri = new();

        public NamespaceTable NamespaceUris { get => _systemContext.NamespaceUris; }

        public ILogger Logger => _logger;

        public bool UseLocalNodeIds { get; set; }
        public Dictionary<string, NodeSetModel> NodeSetModels => _nodesetModels;

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
            foreach (var nodeSetModel in _nodesetModels.Values)
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
            if (!_nodesetModels.TryGetValue(model.ModelUri, out var nodesetModel))
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
                        var requiredModelInfo = new RequiredModelInfo
                        {
                            ModelUri = requiredModel.ModelUri,
                            PublicationDate = requiredModel.GetNormalizedPublicationDate(),
                            Version = requiredModel.Version,
                            AvailableModel = existingNodeSet,
                        };
                        nodesetModel.RequiredModels.Add(requiredModelInfo);
                    }
                }
                _nodesetModels.Add(nodesetModel.ModelUri, nodesetModel);
            }
            return nodesetModel;
        }

        public virtual List<NodeState> ImportUANodeSet(UANodeSet nodeSet)
        {
            var previousNodes = _importedNodes.ToList();
            if (nodeSet.Items?.Any() == true)
            {
                nodeSet.Import(_systemContext, _importedNodes);
            }
            var newlyImportedNodes = _importedNodes.Except(previousNodes).ToList();
            if (newlyImportedNodes.Any())
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
            return NodeModelUtils.JsonEncodeVariant(_systemContext, wrappedValue, dataType, ReencodeExtensionsAsJson, EncodeJsonScalarsAsValue);
        }

        public virtual Variant JsonDecodeVariant(string jsonVariant, DataTypeModel dataType = null)
        {
            dataType ??= this.GetModelForNode<DataTypeModel>(this.GetModelNodeId(DataTypeIds.String));
            var variant = NodeModelUtils.JsonDecodeVariant(jsonVariant, new ServiceMessageContext { NamespaceUris = _systemContext.NamespaceUris }, dataType, EncodeJsonScalarsAsValue);
            return variant;
        }

        public string GetModelBrowseName(QualifiedName browseName)
        {
            if (UseLocalNodeIds)
            {
                return browseName.ToString();
            }
            return $"{NamespaceUris.GetString(browseName.NamespaceIndex)};{browseName.Name}";
        }

        public QualifiedName GetBrowseNameFromModel(string modelBrowseName)
        {
            if (UseLocalNodeIds)
            {
                return QualifiedName.Parse(modelBrowseName);
            }
            var parts = modelBrowseName.Split(new[] { ';' }, 2);
            if (parts.Length == 1)
            {
                return new QualifiedName(parts[0]);
            }
            return new QualifiedName(parts[1], (ushort)NamespaceUris.GetIndex(parts[0]));
        }
    }
}