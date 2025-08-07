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
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;
using static Opc.Ua.Cloud.Library.NodeModel;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public class NodeModelFactoryOpc
    {
        private readonly NodeSetModel _nodesetModel;

        private readonly DefaultOpcUaContext _opcContext;

        private readonly UANodeSet _nodeset;

        private readonly ILogger _logger;

        public NodeModelFactoryOpc(NodeSetModel nodesetModel, UANodeSet nodeset, ILogger logger)
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
                CreateNodeModel(node);
            }
        }

        private void CreateNodeModel(NodeState node)
        {
            if (node is DataTypeState dataType)
            {
                _nodesetModel.DataTypes.Add(CreateDataTypeModel(dataType));
            }
            else if (node is BaseVariableTypeState variableType)
            {
                _nodesetModel.VariableTypes.Add(CreateVariableTypeModel(variableType));
            }
            else if (node is BaseObjectTypeState objectType)
            {
                _nodesetModel.ObjectTypes.Add(CreateObjectTypeModel(objectType));
            }
            else if (node is BaseInterfaceState uaInterface)
            {
                _nodesetModel.Interfaces.Add(CreateInterfaceModel(uaInterface));
            }
            else if (node is BaseObjectState uaObject)
            {
                _nodesetModel.Objects.Add(CreateObjectModel(uaObject));
            }
            else if (node is PropertyState property)
            {
                _nodesetModel.Properties.Add(CreatePropertyModel(property));
            }
            else if (node is BaseDataVariableState dataVariable)
            {
                _nodesetModel.DataVariables.Add(CreateVariableModel(dataVariable));
            }
            else if (node is MethodState methodState)
            {
                _nodesetModel.Methods.Add(CreateMethodModel(methodState));
            }
            else if (node is ReferenceTypeState referenceState)
            {
                _nodesetModel.ReferenceTypes.Add(CreateReferenceTypeModel(referenceState));
            }
            else
            {
                _nodesetModel.UnknownNodes.Add(InitializeNodeModel(new NodeModel(), node));
            }
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
            nodeModel.NodeId = _opcContext.GetExpandedNodeId(opcNode.NodeId);
            nodeModel.NodeIdIdentifier = opcNode.NodeId.Identifier.ToString();

            foreach (NodeStateHierarchyReference reference in _opcContext.GetHierarchyReferences(opcNode))
            {
                NodeModel referenceNodeModel = new() {
                    DisplayName = new LocalizedText(_opcContext.GetExpandedNodeId(reference.ReferenceTypeId)).ToModel(),
                    NodeId = _opcContext.GetExpandedNodeId(reference.ReferenceTypeId),
                    ReferencesNotResolved = true
                };

                NodeModel referenceTypeModel = new() {
                    DisplayName = new LocalizedText(_opcContext.GetExpandedNodeId(reference.ReferenceTypeId)).ToModel(),
                    NodeId = _opcContext.GetExpandedNodeId(reference.ReferenceTypeId),
                    ReferencesNotResolved = true
                };

                nodeModel.AllReferencedNodes = new List<NodeAndReference>() { new NodeAndReference() { Node = referenceNodeModel, ReferenceType = referenceTypeModel } };
            }

            if (!_nodesetModel.AllNodesByNodeId.TryAdd(nodeModel.NodeId, nodeModel))
            {
                throw new ArgumentException($"Duplicate node {nodeModel} for {opcNode} in the nodeset.");
            }
            else
            {
                _logger.LogTrace($"Created node dataTypeModel {nodeModel} for {opcNode}");
            }

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

            objectTypeModel.SuperType = baseType.SuperTypeId?.ToString();
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

        private DataVariableModel CreateVariableModel(BaseVariableState baseVariable)
        {
            DataVariableModel variableModel = InitializeNodeModel(new DataVariableModel(), baseVariable) as DataVariableModel;

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
            return InitializeNodeModel(new MethodModel(), method) as MethodModel;
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
                if (variableTypeNode.WrappedValue.Value is ExtensionObject extensionObject)
                {
                    variableTypeModel.Value = ((System.Xml.XmlNode)extensionObject.Body).OuterXml;
                }
                else
                {
                    variableTypeModel.Value = variableTypeNode.WrappedValue.Value.ToString();
                }
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

                    foreach (StructureField field in sd.Fields)
                    {
                        string symbolicName = null;
                        UADataType uaStruct = _nodeset.Items.FirstOrDefault(n => n.NodeId == dataType.NodeId.ToString()) as UADataType;
                        if (uaStruct != null)
                        {
                            symbolicName = uaStruct?.Definition?.Field?.FirstOrDefault(f => f.Name == field.Name)?.SymbolicName;
                        }

                        var structureField = new DataTypeModel.StructureField {
                            Name = field.Name,
                            SymbolicName = symbolicName,
                            DataType = field.DataType.ToString(),
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
