using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;
using static NpgsqlTypes.NpgsqlTsQuery;
using static Opc.Ua.Cloud.Library.NodeModel;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public class NodeModelFactoryOpc
    {
        private readonly CloudLibNodeSetModel _nodesetModel;

        private readonly DefaultOpcUaContext _opcContext;

        private readonly UANodeSet _nodeset;

        private readonly ILogger _logger;

        public NodeModelFactoryOpc(CloudLibNodeSetModel nodesetModel, UANodeSet nodeset, ILogger logger)
        {
            if (nodesetModel == null)
            {
                throw new ArgumentNullException(nameof(nodesetModel), "NodeSetModel cannot be null.");
            }

            if (nodeset == null)
            {
                throw new ArgumentNullException(nameof(nodeset), "NodeSet cannot be null.");
            }

            _nodesetModel = nodesetModel;
            _nodeset = nodeset;
            _logger = logger;
            _opcContext = new DefaultOpcUaContext(logger);
        }

        public void ImportNodeSet()
        {
            if (_nodeset == null)
            {
                throw new ArgumentNullException(nameof(_nodeset), "NodeSet cannot be null.");
            }

            NodeModelUtils.FixupNodesetVersionFromMetadata(_nodeset, _logger);

            // Ensure the namespace is in the namespace table
            _opcContext.NamespaceUris.GetIndexOrAppend(_nodesetModel.ModelUri);

            if (_nodeset.Items == null)
            {
                _nodeset.Items = [];
            }

            NodeStateCollection importedNodes = [];
            _nodeset.Import(_opcContext.GetSystemContext(), importedNodes);

            foreach (NodeState node in importedNodes)
            {
                NodeModel nodeModel = CreateNodeModel(node, out bool added);

                // check if the node was added to the model
                if ((nodeModel != null) && !added)
                {
                    // not added, so add it to the Unknown Nodes collection
                    if (!_nodesetModel.AllNodesByNodeId.ContainsKey(nodeModel.NodeId))
                    {
                        _nodesetModel.UnknownNodes.Add(nodeModel);
                    }
                }
            }
        }

        private NodeModel CreateNodeModel(NodeState node, out bool added)
        {
            NodeModel nodeModel;
            added = true;

            if (node is DataTypeState dataType)
            {
                nodeModel = CreateDataTypeModel(dataType);
            }
            else if (node is BaseVariableTypeState variableType)
            {
                nodeModel = CreateVariableTypeModel(variableType);
            }
            else if (node is BaseObjectTypeState objectType)
            {
                nodeModel = CreateObjectTypeModel(objectType);
            }
            else if (node is BaseInterfaceState uaInterface)
            {
                nodeModel = CreateInterfaceModel(uaInterface);
            }
            else if (node is BaseObjectState uaObject)
            {
                nodeModel = CreateObjectModel(uaObject);
            }
            else if (node is PropertyState property)
            {
                nodeModel = CreatePropertyModel(property);
            }
            else if (node is BaseDataVariableState dataVariable)
            {
                nodeModel = CreateVariableModel(dataVariable);
            }
            else if (node is MethodState methodState)
            {
                nodeModel = CreateMethodModel(methodState);
            }
            else if (node is ReferenceTypeState referenceState)
            {
                nodeModel = CreateReferenceTypeModel(referenceState);
            }
            else
            {
                if (!(node is ViewState))
                {
                    nodeModel = InitializeNodeModel(new NodeModel(), node);
                }
                else
                {
                    // TODO: Support Views
                    nodeModel = null;
                    added = false;
                    _logger.LogWarning($"Node {node} is a ViewState, which is not supported in the current implementation. Skipping this node.");
                }
            }

            if (added)
            {
                if (!_nodesetModel.AllNodesByNodeId.TryAdd(nodeModel.NodeId, nodeModel))
                {
                    // Node already processed
                    _logger.LogWarning($"Node {nodeModel} was already in the nodeset dataTypeModel.");
                }
                else
                {
                    if (nodeModel is InterfaceModel uaInterface)
                    {
                        _nodesetModel.Interfaces.Add(uaInterface);
                    }
                    else if (nodeModel is ObjectTypeModel objectType)
                    {
                        _nodesetModel.ObjectTypes.Add(objectType);
                    }
                    else if (nodeModel is DataTypeModel uaDataType)
                    {
                        _nodesetModel.DataTypes.Add(uaDataType);
                    }
                    else if (nodeModel is DataVariableModel dataVariable)
                    {
                        _nodesetModel.DataVariables.Add(dataVariable);
                    }
                    else if (nodeModel is VariableTypeModel variableType)
                    {
                        _nodesetModel.VariableTypes.Add(variableType);
                    }
                    else if (nodeModel is ObjectModel uaObject)
                    {
                        _nodesetModel.Objects.Add(uaObject);
                    }
                    else if (nodeModel is PropertyModel property)
                    {
                        _nodesetModel.Properties.Add(property);
                    }
                    else if (nodeModel is MethodModel method)
                    {
                        _nodesetModel.Methods.Add(method);
                    }
                    else if (nodeModel is ReferenceTypeModel referenceType)
                    {
                        _nodesetModel.ReferenceTypes.Add(referenceType);
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected node dataTypeModel type {nodeModel.GetType().FullName} for node {nodeModel}");
                    }
                }
            }

            return nodeModel;
        }

        private NodeModel InitializeNodeModel(NodeModel nodeModel, NodeState opcNode)
        {
            _logger.LogTrace($"Creating node variableModel for {opcNode}");

            nodeModel.DisplayName = opcNode.DisplayName.ToModel();
            nodeModel.BrowseName = _opcContext.GetModelBrowseName(opcNode.BrowseName);
            nodeModel.SymbolicName = opcNode.SymbolicName;
            nodeModel.Description = opcNode.Description.ToModel();
            nodeModel.Documentation = opcNode.NodeSetDocumentation;
            nodeModel.ReleaseStatus = opcNode.ReleaseStatus.ToString();
            nodeModel.ReferencesNotResolved = false;
            nodeModel.NodeSet = _nodesetModel;

            foreach (NodeStateHierarchyReference reference in _opcContext.GetHierarchyReferences(opcNode))
            {
                NodeModel referenceNodeModel = new() {
                    SymbolicName = reference.ToString(),
                    ReferencesNotResolved = true
                };

                NodeModel referenceTypeModel = new() {
                    SymbolicName = reference.ReferenceTypeId.ToString(),
                    ReferencesNotResolved = true
                };

                nodeModel.AllReferencedNodes = new List<NodeAndReference>() { new NodeAndReference() { Node = referenceNodeModel, ReferenceType = referenceTypeModel } };
            }

            _logger.LogTrace($"Created node dataTypeModel {nodeModel} for {opcNode}");

            return nodeModel;
        }


        private InterfaceModel CreateInterfaceModel(BaseInterfaceState baseInterface)
        {
            InterfaceModel interfaceModel = InitializeNodeModel(new InterfaceModel(), baseInterface) as InterfaceModel;

            return interfaceModel;
        }


        private ObjectModel CreateObjectModel(BaseObjectState objState)
        {
            ObjectModel objectModel = InitializeNodeModel(new ObjectModel(), objState) as ObjectModel;

            objectModel.EventNotifier = objState.EventNotifier;

            return objectModel;
        }

        private ObjectTypeModel CreateObjectTypeModel(BaseObjectTypeState baseType)
        {
            ObjectTypeModel objectTypeModel = InitializeNodeModel(new ObjectTypeModel(), baseType) as ObjectTypeModel;

            objectTypeModel.SuperType = baseType.SuperTypeId.ToString();
            objectTypeModel.IsAbstract = baseType.IsAbstract;

            return objectTypeModel;
        }

        private PropertyModel CreatePropertyModel(PropertyState property)
        {
            PropertyModel propertyModel = InitializeNodeModel(new PropertyModel(), property) as PropertyModel;

            if (property.Value != null)
            {
                propertyModel.Value = property.Value.ToString();
            }

            if (property.DataType != null)
            {
                AddDataTypeInfo(propertyModel, property);
            }

            return propertyModel;
        }

        private VariableModel CreateVariableModel(BaseVariableState baseVariable)
        {
            VariableModel variableModel = InitializeNodeModel(new VariableModel(), baseVariable) as VariableModel;

            if (baseVariable.Value != null)
            {
                variableModel.Value = baseVariable.Value.ToString();
            }

            if (baseVariable.DataType != null)
            {
                AddDataTypeInfo(variableModel, baseVariable);
            }

            if (baseVariable.AccessLevelEx != 1)
            {
                variableModel.AccessLevel = baseVariable.AccessLevelEx;
            }

            if (baseVariable.AccessRestrictions != 0)
            {
                variableModel.AccessRestrictions = (ushort)(baseVariable.AccessRestrictions == null ? 0 : baseVariable.AccessRestrictions);
            }

            if (baseVariable.WriteMask != 0)
            {
                variableModel.WriteMask = (uint)baseVariable.WriteMask;
            }

            if (baseVariable.UserWriteMask != 0)
            {
                variableModel.UserWriteMask = (uint)baseVariable.UserWriteMask;
            }

            if (baseVariable.MinimumSamplingInterval != 0)
            {
                variableModel.MinimumSamplingInterval = baseVariable.MinimumSamplingInterval;
            }

            var invalidBrowseNameOnTypeInformation = variableModel.Properties.Where(p =>
                p.BrowseName.EndsWith(BrowseNames.EnumValues, false, CultureInfo.InvariantCulture) && p.BrowseName != _opcContext.GetModelBrowseName(BrowseNames.EnumValues)
             || p.BrowseName.EndsWith(BrowseNames.EnumStrings, false, CultureInfo.InvariantCulture) && p.BrowseName != _opcContext.GetModelBrowseName(BrowseNames.EnumStrings)
             || p.BrowseName.EndsWith(BrowseNames.OptionSetValues, false, CultureInfo.InvariantCulture) && p.BrowseName != _opcContext.GetModelBrowseName(BrowseNames.OptionSetValues)
            );

            if (invalidBrowseNameOnTypeInformation.Any())
            {
                _opcContext.Logger.LogWarning($"Found type definition node with browsename in non-default namespace: {string.Join("", invalidBrowseNameOnTypeInformation.Select(ti => ti.BrowseName))}");
            }

            if (string.IsNullOrEmpty(variableModel.NodeSet.XmlSchemaUri) && baseVariable.TypeDefinitionId == VariableTypeIds.DataTypeDictionaryType)
            {
                var namespaceUriModelBrowseName = _opcContext.GetModelBrowseName(BrowseNames.NamespaceUri);
                var xmlNamespaceVariable = variableModel.Properties.FirstOrDefault(dv => dv.BrowseName == namespaceUriModelBrowseName);
                if (variableModel.Parent?.NodeId == _opcContext.GetExpandedNodeId(ObjectIds.XmlSchema_TypeSystem))
                {
                    if (xmlNamespaceVariable != null && !string.IsNullOrEmpty(xmlNamespaceVariable.Value))
                    {
                        variableModel.NodeSet.XmlSchemaUri = xmlNamespaceVariable.Value;
                    }
                }
            }

            return variableModel;
        }

        private MethodModel CreateMethodModel(MethodState method)
        {
            MethodModel methodModel = InitializeNodeModel(new MethodModel(), method) as MethodModel;

            foreach (var reference in _opcContext.GetHierarchyReferences(method).Where(r => r.ReferenceTypeId == ReferenceTypeIds.HasProperty))
            {
                // TODO: Handle InputArguments and OutputArguments properties
                //if (referencedNode?.BrowseName == "InputArguments" || referencedNode?.BrowseName == "OutputArguments")
                //{
                //    if (referencedNode is PropertyState argumentProp && argumentProp.Value is ExtensionObject[] arguments)
                //    {
                //        var argumentInfo = new PropertyState<Argument[]>(method) {
                //            NodeId = argumentProp.NodeId,
                //            TypeDefinitionId = argumentProp.TypeDefinitionId,
                //            ModellingRuleId = argumentProp.ModellingRuleId,
                //            DataType = argumentProp.DataType,
                //        };

                //        argumentInfo.Value = new Argument[arguments.Length];
                //        for (int arg = 0; arg < arguments.Length; arg++)
                //        {
                //            argumentInfo.Value[arg] = arguments[arg].Body as Argument;
                //        }

                //        if (referencedNode?.BrowseName == "InputArguments")
                //        {
                //            method.InputArguments = argumentInfo;
                //        }
                //        else
                //        {
                //            method.OutputArguments = argumentInfo;
                //        }
                //    }
                //}
            }

            return methodModel;
        }

        private void AddDataTypeInfo(VariableModel variableModel, BaseVariableState variableNode)
        {
            AddDataTypeInfo(variableModel, $"{variableNode.GetType()} {variableNode}", variableNode.DataType, variableNode.ValueRank, variableNode.ArrayDimensions, variableNode.WrappedValue);
        }

        private void AddDataTypeInfo(VariableModel variableModel, string variableNodeDiagInfo, NodeId dataTypeNodeId, int valueRank, ReadOnlyList<uint> arrayDimensions, Variant wrappedValue)
        {
            variableModel.DataType = dataTypeNodeId.ToString();

            if (valueRank != -1)
            {
                variableModel.ValueRank = valueRank;
                if ((arrayDimensions != null) && (arrayDimensions.Count > 0))
                {
                    variableModel.ArrayDimensions = string.Join(",", arrayDimensions);
                }
            }
        }

        private void AddDataTypeInfo(VariableTypeModel variableTypeModel, BaseVariableTypeState variableTypeNode)
        {
            variableTypeModel.DataType = variableTypeNode.DataType.ToString();

            if (variableTypeNode.ValueRank != -1)
            {
                variableTypeModel.ValueRank = variableTypeNode.ValueRank;
                if ((variableTypeNode.ArrayDimensions != null) && (variableTypeNode.ArrayDimensions.Count > 0))
                {
                    variableTypeModel.ArrayDimensions = string.Join(",", variableTypeNode.ArrayDimensions);
                }
            }

            if (variableTypeNode.WrappedValue.Value != null)
            {
                throw new NotImplementedException($"Wrapped value {variableTypeNode.WrappedValue.Value} for {variableTypeNode.GetType()} {variableTypeNode} is not supported. Please report this to the UA Cloud Library team.");
            }
        }

        private VariableTypeModel CreateVariableTypeModel(BaseVariableTypeState variableType)
        {
            VariableTypeModel variableModel = InitializeNodeModel(new VariableTypeModel(), variableType) as VariableTypeModel;

            AddDataTypeInfo(variableModel, variableType);

            return variableModel;
        }

        private DataTypeModel CreateDataTypeModel(DataTypeState dataType)
        {
            DataTypeModel dataTypeModel = InitializeNodeModel(new DataTypeModel(), dataType) as DataTypeModel;

            // check for complex type (structure or enum)
            if (dataType.DataTypeDefinition?.Body != null)
            {
                StructureDefinition sd = dataType.DataTypeDefinition.Body as StructureDefinition;
                if (sd != null)
                {
                    dataTypeModel.StructureFields = new List<DataTypeModel.StructureField>();
                    int order = 0;

                    foreach (var field in sd.Fields)
                    {
                        if (dataType is DataTypeState)
                        {
                            DataTypeModel fieldDataTypeModel = CreateDataTypeModel(dataType as DataTypeState);
                            if (fieldDataTypeModel == null)
                            {
                                throw new ArgumentException($"Unable to resolve data type {dataType.DisplayName}");
                            }

                            string symbolicName = null;
                            UADataType uaStruct = _nodeset.Items.FirstOrDefault(n => n.NodeId == dataType.NodeId.ToString()) as UADataType;
                            if (uaStruct != null)
                            {
                                symbolicName = uaStruct?.Definition?.Field?.FirstOrDefault(f => f.Name == field.Name)?.SymbolicName;
                            }

                            var structureField = new DataTypeModel.StructureField {
                                Name = field.Name,
                                SymbolicName = symbolicName,
                                DataType = fieldDataTypeModel,
                                ValueRank = field.ValueRank != -1 ? field.ValueRank : null,
                                ArrayDimensions = field.ArrayDimensions != null && field.ArrayDimensions.Any() ? string.Join(",", field.ArrayDimensions) : null,
                                MaxStringLength = field.MaxStringLength != 0 ? field.MaxStringLength : null,
                                Description = field.Description.ToModel(),
                                IsOptional = field.IsOptional && sd.StructureType == StructureType.StructureWithOptionalFields,
                                AllowSubTypes = field.IsOptional && (sd.StructureType == StructureType.StructureWithSubtypedValues || sd.StructureType == StructureType.UnionWithSubtypedValues),
                                FieldOrder = order++,
                            };

                            dataTypeModel.StructureFields.Add(structureField);
                        }
                        else
                        {
                            if (dataType == null)
                            {
                                throw new ArgumentException($"Unable to find node state for data type {field.DataType} in {dataType}");
                            }
                            else
                            {
                                throw new ArgumentException($"Unexpected node state {dataType?.GetType()?.FullName} for data type {field.DataType} in {dataType}");
                            }
                        }
                    }
                }
                else
                {
                    var enumFields = dataType.DataTypeDefinition.Body as EnumDefinition;
                    if (enumFields != null)
                    {
                        dataTypeModel.IsOptionSet = enumFields.IsOptionSet;
                        dataTypeModel.EnumFields = new List<DataTypeModel.UaEnumField>();

                        UADataType uaEnum = _nodeset.Items.FirstOrDefault(n => n.NodeId == dataType.NodeId) as UADataType;
                        foreach (var field in enumFields.Fields)
                        {
                            string symbolicName = null;
                            if (uaEnum != null)
                            {
                                symbolicName = uaEnum?.Definition?.Field?.FirstOrDefault(f => f.Name == field.Name)?.SymbolicName;
                            }

                            var enumField = new DataTypeModel.UaEnumField {
                                Name = field.Name,
                                DisplayName = field.DisplayName.ToModel(),
                                Value = field.Value,
                                Description = field.Description.ToModel(),
                                SymbolicName = symbolicName,
                            };

                            dataTypeModel.EnumFields.Add(enumField);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown data type definition in {dataType}");
                    }
                }
            }

            return dataTypeModel;
        }

        private ReferenceTypeModel CreateReferenceTypeModel(ReferenceTypeState referenceType)
        {
            ReferenceTypeModel referenceTypeModel = InitializeNodeModel(new ReferenceTypeModel(), referenceType) as ReferenceTypeModel;

            referenceTypeModel.InverseName = referenceType.InverseName?.ToModel();
            referenceTypeModel.Symmetric = referenceType.Symmetric;

            return referenceTypeModel;
        }
    }
}
