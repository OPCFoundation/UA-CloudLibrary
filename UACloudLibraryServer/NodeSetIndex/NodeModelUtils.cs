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

        public static (string Json, bool IsScalar) JsonEncodeVariant(ISystemContext systemContext, Variant value, DataTypeModel dataType, bool reencodeExtensionsAsJson = false, bool encodeJsonScalarsAsValues = false)
        {
            bool isScalar = false;

            ServiceMessageContext context;
            if (systemContext != null)
            {
                context = new ServiceMessageContext { NamespaceUris = systemContext.NamespaceUris, Factory = systemContext.EncodeableFactory };
            }
            else
            {
                context = ServiceMessageContext.GlobalContext;
            }
            if (reencodeExtensionsAsJson)
            {
                if (dataType != null && systemContext.EncodeableFactory is DynamicEncodeableFactory lookupContext)
                {
                    lookupContext.AddEncodingsForDataType(dataType, systemContext.NamespaceUris);
                }

                // Reencode extension objects as JSON
                if (value.Value is ExtensionObject extObj && extObj.Encoding == ExtensionObjectEncoding.Xml && extObj.Body is XmlElement extXmlBody)
                {
                    var xmlDecoder = new XmlDecoder(extXmlBody, context);
                    var parsedBody = xmlDecoder.ReadExtensionObjectBody(extObj.TypeId);
                    value.Value = new ExtensionObject(extObj.TypeId, parsedBody);
                }
                else if (value.Value is ExtensionObject[] extObjList && extObjList.Any(e => e.Encoding == ExtensionObjectEncoding.Xml && e.Body is XmlElement))
                {
                    var newExtObjList = new ExtensionObject[extObjList.Length];
                    int i = 0;
                    bool bReencoded = false;
                    foreach (var extObj2 in extObjList)
                    {
                        if (extObj2.Encoding == ExtensionObjectEncoding.Xml && extObj2.Body is XmlElement extObj2XmlBody)
                        {
                            var xmlDecoder = new XmlDecoder(extObj2XmlBody, context);
                            var parsedBody = xmlDecoder.ReadExtensionObjectBody(extObj2.TypeId);
                            newExtObjList[i] = new ExtensionObject(extObj2.TypeId, parsedBody);
                            bReencoded = true;
                        }
                        else
                        {
                            newExtObjList[i] = extObj2;
                        }
                        i++;
                    }
                    if (bReencoded)
                    {
                        value.Value = newExtObjList;
                    }
                }
                else if (value.Value is byte[] byteArray && value.TypeInfo.BuiltInType == BuiltInType.ByteString && dataType?.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Byte}") == true)
                {
                    // The XML decoder returns byte arrays as a bytestring variant: fix it up so we don't get a base64 encoded JSON value
                    value = new Variant(byteArray, new TypeInfo(BuiltInType.Byte, ValueRanks.OneDimension));
                }
            }

            using (var encoder = new JsonEncoder(context, true))
            {
                encoder.ForceNamespaceUri = true;
                encoder.ForceNamespaceUriForIndex1 = true;
                encoder.WriteVariant("Value", value);

                var encodedVariant = encoder.CloseAndReturnText();
                var parsedValue = JsonConvert.DeserializeObject<JObject>(encodedVariant, new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None });

                string encodedValue;
                NodeModelOpcExtensions.JsonValueType jsonValueType;
                if (encodeJsonScalarsAsValues && dataType != null &&
                    ((jsonValueType = dataType.GetJsonValueType()) == NodeModelOpcExtensions.JsonValueType.Value || jsonValueType == NodeModelOpcExtensions.JsonValueType.String))
                {
                    isScalar = true;
                    if (parsedValue["Value"]["Body"] is JValue jValue)
                    {
                        if (jValue.Value is string stringValue)
                        {
                            encodedValue = stringValue;
                        }
                        else if (jValue.Value is bool boolValue)
                        {
                            // Ensure proper casing, ToString() return True/False vs. json's true/false
                            encodedValue = JsonConvert.SerializeObject(jValue, Newtonsoft.Json.Formatting.None);
                        }
                        else
                        {
                            encodedValue = JsonConvert.SerializeObject(jValue, Newtonsoft.Json.Formatting.None);
                            encodedValue = encodedValue.Trim('"');
                            var encodedValue2 = jValue.Value?.ToString();
                            if (encodedValue != encodedValue2 && !(jValue.Value is DateTime))
                            {

                            }
                        }
                    }
                    else if (parsedValue["Value"]["Body"] is JArray jArray)
                    {
                        encodedValue = JsonConvert.SerializeObject(jArray, Newtonsoft.Json.Formatting.None);
                    }
                    else
                    {
                        encodedValue = null;
                    }
                    if (encodedValue.Length >= 2 && encodedValue.StartsWith("\"") && encodedValue.EndsWith("'\""))
                    {
                        encodedValue = encodedValue.Substring(1, encodedValue.Length - 2);
                    }
                }
                else
                {
                    encodedValue = parsedValue["Value"]?.ToString(Newtonsoft.Json.Formatting.None);
                }

                return (encodedValue, isScalar);
            }
        }

        public static Variant JsonDecodeVariant(string jsonVariant, IServiceMessageContext context, DataTypeModel dataType = null, bool EncodeJsonScalarsAsString = false)
        {
            if (jsonVariant == null)
            {
                return Variant.Null;
            }

            if ((jsonVariant?.TrimStart()?.StartsWith("{\"Value\"") == false))
            {
                NodeModelOpcExtensions.JsonValueType? jsonValueType;
                if (EncodeJsonScalarsAsString && ((jsonValueType = dataType?.GetJsonValueType()) == NodeModelOpcExtensions.JsonValueType.Value || jsonValueType == NodeModelOpcExtensions.JsonValueType.String))
                {
                    uint? dataTypeId = null;
                    if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Enumeration}"))
                    {
                        dataTypeId = DataTypes.Int32;
                    }
                    else
                    {
                        var dtNodeId = ExpandedNodeId.Parse(dataType.NodeId, context.NamespaceUris);
                        var builtInType = TypeInfo.GetBuiltInType(dtNodeId, new PartialTypeTree(dataType, context.NamespaceUris));
                        if (builtInType != BuiltInType.Null)
                        {
                            dataTypeId = (uint)builtInType;
                        }

                        else
                        {
                            if (dtNodeId.IdType == IdType.Numeric && dtNodeId.NamespaceIndex == 0)
                            {
                                dataTypeId = (uint)dtNodeId.Identifier;
                            }
                        }
                    }
                    if (dataTypeId != null)
                    {
                        // TODO more reliable check for array (handle a scalar string that starts with [ ).
                        if (jsonValueType == NodeModelOpcExtensions.JsonValueType.String && !jsonVariant.StartsWith("["))
                        {
                            // encode and quote it
                            jsonVariant = JsonConvert.ToString(jsonVariant);
                        }
                        jsonVariant = $"{{\"Value\":{{\"Type\":{dataTypeId},\"Body\":{jsonVariant}}}}}";
                    }
                }
                else
                {
                    jsonVariant = $"{{\"Value\":{jsonVariant}";
                }
            }
            using (var decoder = new JsonDecoder(jsonVariant, context))
            {
                var variant = decoder.ReadVariant("Value");
                return variant;
            }
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
            } while (type != null);
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
