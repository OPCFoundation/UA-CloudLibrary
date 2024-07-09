using Opc.Ua;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Opc.Ua.Export;

namespace CESMII.OpcUa.NodeSetModel.Factory.Opc
{
    public interface IOpcUaContext
    {
        // OPC utilities
        NamespaceTable NamespaceUris { get; }
        /// <summary>
        /// NodeIds in the NodeModel will not use namespace URIs ("nsu=", absolute NodeIds) but namespace indices ("ns=", local NodeIds). 
        /// Use only if the NodeModel is generated in the context of a specific OPC server, or in a specific set of nodesets that are loaded in a specific order.
        /// </summary>
        bool UseLocalNodeIds { get; }
        /// <summary>
        /// /
        /// </summary>
        /// <param name="nodeId"></param>
        /// 
        /// <returns></returns>
        string GetModelNodeId(NodeId nodeId);

        // OPC NodeState cache
        NodeState GetNode(NodeId nodeId);
        NodeState GetNode(ExpandedNodeId expandedNodeId);
        List<NodeStateHierarchyReference> GetHierarchyReferences(NodeState nodeState);

        // NodesetModel cache
        NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true);
        TNodeModel GetModelForNode<TNodeModel>(string nodeId) where TNodeModel : NodeModel;
        ILogger Logger { get; }
        (string Json, bool IsScalar) JsonEncodeVariant(Variant wrappedValue, DataTypeModel dataType = null);
        Variant JsonDecodeVariant(string jsonVariant, DataTypeModel dataType = null);
        List<NodeState> ImportUANodeSet(UANodeSet nodeSet);
        UANodeSet GetUANodeSet(string modeluri);
        
        string GetModelBrowseName(QualifiedName browseName);
        QualifiedName GetBrowseNameFromModel(string modelBrowseName);

        Dictionary<string, NodeSetModel> NodeSetModels { get; }
    }
}