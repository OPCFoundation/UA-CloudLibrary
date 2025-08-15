/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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
