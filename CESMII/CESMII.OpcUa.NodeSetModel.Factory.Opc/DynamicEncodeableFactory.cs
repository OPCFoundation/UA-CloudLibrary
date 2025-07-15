using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Opc.Ua.Client.ComplexTypes;
using System.Runtime.Serialization;
using R = System.Reflection;
using System.Reflection.Emit;
//using Opc.Ua.Gds;

namespace CESMII.OpcUa.NodeSetModel.Export.Opc
{

    /// <summary>
    /// 
    /// </summary>
    public interface IDynamicEncodeableFactory
    {
        public DataTypeModel GetDataTypeForEncoding(ExpandedNodeId typeId);
        Dictionary<string, ExpandedNodeId> AddEncodingsForDataType(DataTypeModel dataType, NamespaceTable namespaceUris);
    }

    public class DynamicEncodeableFactory : EncodeableFactory, IDynamicEncodeableFactory
    {
        Dictionary<ExpandedNodeId, DataTypeModel> _dynamicDataTypes = new();

        public DynamicEncodeableFactory(IEncodeableFactory factory) : base(factory)
        {
        }

        public DataTypeModel GetDataTypeForEncoding(ExpandedNodeId typeId)
        {
            if (typeId != null)
            {
                if (_dynamicDataTypes.TryGetValue(typeId, out var dataType))
                {
                    return dataType;
                }
            }
            return null;
        }

        public Dictionary<string, ExpandedNodeId> AddEncodingsForDataType(DataTypeModel dataType, NamespaceTable namespaceUris)
        {
            bool bTypeAlreadyProcessed = false;
            var dataTypeExpandedNodeId = ExpandedNodeId.Parse(dataType.NodeId);
            dataTypeExpandedNodeId = NormalizeNodeIdForEncodableFactory(dataTypeExpandedNodeId, namespaceUris);

            if (_dynamicDataTypes.ContainsKey(dataTypeExpandedNodeId))
            {
                // Used later to break recursion for recursive data structures
                bTypeAlreadyProcessed = true;
            }
            BuiltInType builtInType = GetBuiltInType(dataType, namespaceUris);
            if (builtInType != BuiltInType.Null && builtInType != BuiltInType.ExtensionObject)
            {
                return null;
            }

            _dynamicDataTypes[dataTypeExpandedNodeId] = dataType;

            var encodingsDict = new Dictionary<string, ExpandedNodeId>();
            var encodings = dataType.OtherReferencedNodes.Where(rn => rn.ReferenceType?.NodeId == new ExpandedNodeId(ReferenceTypeIds.HasEncoding, Namespaces.OpcUa).ToString());
            foreach(var encoding in encodings)
            {
                var encodingExpandedNodeId = ExpandedNodeId.Parse(encoding.Node.NodeId);
                encodingExpandedNodeId = NormalizeNodeIdForEncodableFactory(encodingExpandedNodeId, namespaceUris);

                var encodingName = encoding.Node.BrowseName.Replace($"{Namespaces.OpcUa};", "");
                if (!encodingsDict.ContainsKey(encodingName))
                {
                    encodingsDict.Add(encodingName, encodingExpandedNodeId);
                }
                if (!EncodeableTypes.ContainsKey(encodingExpandedNodeId))
                {
                    this.AddEncodeableType(encodingExpandedNodeId, typeof(DynamicComplexType));
                    _dynamicDataTypes[encodingExpandedNodeId] = dataType;
                }
            }
            if (!this.EncodeableTypes.ContainsKey(dataTypeExpandedNodeId))
            {
                this.AddEncodeableType(dataTypeExpandedNodeId, typeof(DynamicComplexType));
            }

            if (!bTypeAlreadyProcessed && dataType.StructureFields?.Any() == true)
            {
                foreach (var field in dataType.StructureFields)
                {
                    AddEncodingsForDataType(field.DataType as DataTypeModel, namespaceUris);
                }
            }
            return encodingsDict;
        }

        public static ExpandedNodeId NormalizeNodeIdForEncodableFactory(ExpandedNodeId expandedNodeId, NamespaceTable namespaceUris)
        {
            // check for default namespace.
            if (expandedNodeId.NamespaceUri == Namespaces.OpcUa)
            {
                // EncodableFactory expects namespace 0 nodeids to have no URI, and all others to provide the URI
                expandedNodeId = NodeId.ToExpandedNodeId(ExpandedNodeId.ToNodeId(expandedNodeId, namespaceUris), namespaceUris);
            }
            return expandedNodeId;
        }

        public static BuiltInType GetBuiltInType(DataTypeModel dataType, NamespaceTable namespaceUris)
        {
            var dtNodeId = ExpandedNodeId.Parse(dataType.NodeId, namespaceUris);
            var builtInType = TypeInfo.GetBuiltInType(dtNodeId);
            if (builtInType == BuiltInType.Null && dataType.SuperType != null)
            {
                var superTypeBuiltInType = GetBuiltInType(dataType.SuperType as DataTypeModel, namespaceUris);
                if (superTypeBuiltInType == BuiltInType.ExtensionObject || superTypeBuiltInType == BuiltInType.Enumeration)
                {
                    return BuiltInType.Null;
                }
                return superTypeBuiltInType;
            }
            return builtInType;
        }

    }

}