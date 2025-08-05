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

        private ILogger _logger;

        public NamespaceTable NamespaceUris { get => _systemContext.NamespaceUris; }

        public ILogger Logger => _logger;

        public DefaultOpcUaContext(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;

            var namespaceTable = new NamespaceTable();
            namespaceTable.GetIndexOrAppend(Namespaces.OpcUa);

            var typeTable = new TypeTable(namespaceTable);

            _systemContext = new SystemContext() {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable
            };
        }

        public SystemContext GetSystemContext()
        {
            return _systemContext;
        }

        public string GetExpandedNodeId(NodeId nodeId)
        {
            string namespaceUri = _systemContext.NamespaceUris.GetString(nodeId.NamespaceIndex);
            if (string.IsNullOrEmpty(namespaceUri))
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid, "Namespace Index ({0}) for node id {1} is not in the namespace table.", nodeId.NamespaceIndex, nodeId);
            }

            return new ExpandedNodeId(nodeId, namespaceUri).ToString();
        }

        public List<NodeStateHierarchyReference> GetHierarchyReferences(NodeState nodeState)
        {
            var hierarchy = new Dictionary<NodeId, string>();
            var references = new List<NodeStateHierarchyReference>();
            nodeState.GetHierarchyReferences(_systemContext, null, hierarchy, references);

            return references;
        }

        public string GetModelBrowseName(QualifiedName browseName)
        {
            return $"{NamespaceUris.GetString(browseName.NamespaceIndex)};{browseName.Name}";
        }
    }
}
