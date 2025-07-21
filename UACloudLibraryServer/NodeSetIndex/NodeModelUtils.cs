using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public static class NodeModelUtils
    {
        public static string GetNamespaceFromNodeId(string nodeId)
        {
            var parsedNodeId = ExpandedNodeId.Parse(nodeId);
            var namespaceUri = parsedNodeId.NamespaceUri;
            return namespaceUri;
        }

        /// <summary>
        /// Reads a missing nodeset version from a NamespaceVersion object
        /// </summary>
        /// <param name="nodeSet"></param>
        public static void FixupNodesetVersionFromMetadata(Export.UANodeSet nodeSet, ILogger logger)
        {
            if (nodeSet?.Models == null)
            {
                return;
            }

            foreach (var model in nodeSet.Models)
            {
                if (string.IsNullOrEmpty(model.Version))
                {
                    var namespaceVersionObject = nodeSet.Items?.FirstOrDefault(n => n is Export.UAVariable && n.BrowseName == BrowseNames.NamespaceVersion) as Export.UAVariable;
                    var version = namespaceVersionObject?.Value?.InnerText;
                    if (!string.IsNullOrEmpty(version))
                    {
                        model.Version = version;
                        if (logger != null)
                        {
                            logger.LogWarning($"Nodeset {model.ModelUri} did not specify a version, but contained a NamespaceVersion property with value {version}.");
                        }
                    }
                }
            }
        }
    }

    public class PartialTypeTree : ITypeTable
    {
        private DataTypeModel _dataType;
        private NamespaceTable _namespaceUris;

        public PartialTypeTree(DataTypeModel dataType, NamespaceTable namespaceUris)
        {
            this._dataType = dataType;
            this._namespaceUris = namespaceUris;
        }

        public NodeId FindSuperType(NodeId typeId)
        {
            var type = this._dataType;

            do
            {
                if (ExpandedNodeId.Parse(type.NodeId, _namespaceUris) == typeId)
                {
                    return ExpandedNodeId.Parse(type.SuperType.NodeId, _namespaceUris);
                }
                type = type.SuperType as DataTypeModel;
            }
            while (type != null);

            return null;
        }

        public Task<NodeId> FindSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
        {
            return Task.FromResult(FindSuperType(typeId));
        }

        public NodeId FindDataTypeId(ExpandedNodeId encodingId)
        {
            throw new NotImplementedException();
        }

        public NodeId FindDataTypeId(NodeId encodingId)
        {
            throw new NotImplementedException();
        }

        public NodeId FindReferenceType(QualifiedName browseName)
        {
            throw new NotImplementedException();
        }

        public QualifiedName FindReferenceTypeName(NodeId referenceTypeId)
        {
            throw new NotImplementedException();
        }

        public IList<NodeId> FindSubTypes(ExpandedNodeId typeId)
        {
            throw new NotImplementedException();
        }

        public NodeId FindSuperType(ExpandedNodeId typeId)
        {
            throw new NotImplementedException();
        }
        public Task<NodeId> FindSuperTypeAsync(ExpandedNodeId typeId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public bool IsEncodingFor(NodeId expectedTypeId, ExtensionObject value)
        {
            throw new NotImplementedException();
        }

        public bool IsEncodingFor(NodeId expectedTypeId, object value)
        {
            throw new NotImplementedException();
        }

        public bool IsEncodingOf(ExpandedNodeId encodingId, ExpandedNodeId datatypeId)
        {
            throw new NotImplementedException();
        }

        public bool IsKnown(ExpandedNodeId typeId)
        {
            throw new NotImplementedException();
        }

        public bool IsKnown(NodeId typeId)
        {
            throw new NotImplementedException();
        }

        public bool IsTypeOf(ExpandedNodeId subTypeId, ExpandedNodeId superTypeId)
        {
            throw new NotImplementedException();
        }

        public bool IsTypeOf(NodeId subTypeId, NodeId superTypeId)
        {
            throw new NotImplementedException();
        }
    }
}
