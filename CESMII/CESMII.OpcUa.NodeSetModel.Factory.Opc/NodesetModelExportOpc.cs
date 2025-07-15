using Opc.Ua;
using ua = Opc.Ua;
using uaExport = Opc.Ua.Export;

using System;
using System.Collections.Generic;
using System.Linq;

using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Opc.Ua.Export;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using System.Xml;
using System.Globalization;
using Newtonsoft.Json;

namespace CESMII.OpcUa.NodeSetModel.Export.Opc
{

    public class NodeModelExportOpc : NodeModelExportOpc<NodeModel>
    {

    }
    public class NodeModelExportOpc<T> where T : NodeModel, new()
    {
        protected T _model;

        public static (UANode ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode(NodeModel model, ExportContext context)
        {
            if (model is InterfaceModel uaInterface)
            {
                return new InterfaceModelExportOpc { _model = uaInterface }.GetUANode<UANode>(context);
            }
            else if (model is ObjectTypeModel objectType)
            {
                return new ObjectTypeModelExportOpc { _model = objectType, }.GetUANode<UANode>(context);
            }
            else if (model is VariableTypeModel variableType)
            {
                return new VariableTypeModelExportOpc { _model = variableType, }.GetUANode<UANode>(context);
            }
            else if (model is DataTypeModel dataType)
            {
                return new DataTypeModelExportOpc { _model = dataType, }.GetUANode<UANode>(context);
            }
            else if (model is DataVariableModel dataVariable)
            {
                return new DataVariableModelExportOpc { _model = dataVariable, }.GetUANode<UANode>(context);
            }
            else if (model is PropertyModel property)
            {
                return new PropertyModelExportOpc { _model = property, }.GetUANode<UANode>(context);
            }
            else if (model is ObjectModel uaObject)
            {
                return new ObjectModelExportOpc { _model = uaObject, }.GetUANode<UANode>(context);
            }
            else if (model is MethodModel uaMethod)
            {
                return new MethodModelExportOpc { _model = uaMethod, }.GetUANode<UANode>(context);
            }
            else if (model is ReferenceTypeModel referenceType)
            {
                return new ReferenceTypeModelExportOpc { _model = referenceType, }.GetUANode<UANode>(context);
            }
            throw new Exception($"Unexpected node model {model.GetType()}");
        }

        public virtual (TUANode ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<TUANode>(ExportContext context) where TUANode : UANode, new()
        {
            var nodeIdForExport = GetNodeIdForExport(_model.NodeId, context);
            if (context._exportedSoFar.TryGetValue(nodeIdForExport, out var existingNode))
            {
                return ((TUANode)existingNode, null, false);
            }
            var node = new TUANode
            {
                Description = _model.Description?.ToExport()?.ToArray(),
                BrowseName = GetBrowseNameForExport(context.NamespaceUris),
                SymbolicName = _model.SymbolicName,
                DisplayName = _model.DisplayName?.ToExport()?.ToArray(),
                NodeId = nodeIdForExport,
                Documentation = _model.Documentation,
                Category = _model.Categories?.ToArray(),
            };
            context._exportedSoFar.Add(nodeIdForExport, node);
            if (!string.IsNullOrEmpty(_model.ReleaseStatus))
            {
                if (Enum.TryParse<ReleaseStatus>(_model.ReleaseStatus, out var releaseStatus))
                {
                    node.ReleaseStatus = releaseStatus;
                }
                else
                {
                    throw new Exception($"Invalid release status '{_model.ReleaseStatus}' on {_model}");
                }
            }

            var references = new List<Reference>();
            foreach (var property in _model.Properties)
            {
                if (_model is DataTypeModel &&
                    (property.BrowseName.EndsWith(BrowseNames.EnumValues)
                    || property.BrowseName.EndsWith(BrowseNames.EnumStrings)
                    || property.BrowseName.EndsWith(BrowseNames.OptionSetValues)))
                {
                    // Property will get generated during data type export
                    continue;
                }
                context.NamespaceUris.GetIndexOrAppend(property.Namespace);
                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.HasProperty);
                if (GetOtherReferenceWithDerivedReferenceType(property, referenceTypeId) == null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context),
                        Value = GetNodeIdForExport(property.NodeId, context),
                    });
                }
            }
            foreach (var uaObject in this._model.Objects)
            {
                context.NamespaceUris.GetIndexOrAppend(uaObject.Namespace);
                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.HasComponent);
                if (GetOtherReferenceWithDerivedReferenceType(uaObject, referenceTypeId) == null)
                {
                    // Only add if not also covered in OtherReferencedNodes (will be added later)
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(referenceTypeId, context),
                        Value = GetNodeIdForExport(uaObject.NodeId, context),
                    });
                }
            }
            foreach (var nodeRef in this._model.OtherReferencedNodes)
            {
                context.NamespaceUris.GetIndexOrAppend(nodeRef.Node.Namespace);
                context.NamespaceUris.GetIndexOrAppend(NodeModelUtils.GetNamespaceFromNodeId(nodeRef.ReferenceType?.NodeId));

                references.Add(new Reference
                {
                    ReferenceType = GetNodeIdForExport(nodeRef.ReferenceType?.NodeId, context),
                    Value = GetNodeIdForExport(nodeRef.Node.NodeId, context),
                });
            }
            foreach (var inverseNodeRef in this._model.OtherReferencingNodes)
            {
                context.NamespaceUris.GetIndexOrAppend(inverseNodeRef.Node.Namespace);
                context.NamespaceUris.GetIndexOrAppend(NodeModelUtils.GetNamespaceFromNodeId(inverseNodeRef.ReferenceType?.NodeId));

                var inverseRef = new Reference
                {
                    ReferenceType = GetNodeIdForExport(inverseNodeRef.ReferenceType?.NodeId, context),
                    Value = GetNodeIdForExport(inverseNodeRef.Node.NodeId, context),
                    IsForward = false,
                };
                if (!references.Any(r => r.IsForward == false && r.ReferenceType == inverseRef.ReferenceType && r.Value == inverseRef.Value))
                {
                    // TODO ensure we pick the most derived reference type
                    references.Add(inverseRef);
                }
            }
            foreach (var uaInterface in this._model.Interfaces)
            {
                context.NamespaceUris.GetIndexOrAppend(uaInterface.Namespace);
                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.HasInterface);
                if (GetOtherReferenceWithDerivedReferenceType(uaInterface, referenceTypeId) == null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(referenceTypeId, context),
                        Value = GetNodeIdForExport(uaInterface.NodeId, context),
                    });
                }
            }
            foreach (var method in this._model.Methods)
            {
                context.NamespaceUris.GetIndexOrAppend(method.Namespace);

                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.HasComponent);
                if (GetOtherReferenceWithDerivedReferenceType(method, referenceTypeId) == null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(referenceTypeId, context),
                        Value = GetNodeIdForExport(method.NodeId, context),
                    });
                }
            }
            foreach (var uaEvent in this._model.Events)
            {
                context.NamespaceUris.GetIndexOrAppend(uaEvent.Namespace);
                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.GeneratesEvent);
                if (GetOtherReferenceWithDerivedReferenceType(uaEvent, referenceTypeId) == null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(referenceTypeId, context),
                        Value = GetNodeIdForExport(uaEvent.NodeId, context),
                    });
                }
            }
            foreach (var variable in this._model.DataVariables)
            {
                context.NamespaceUris.GetIndexOrAppend(variable.Namespace);
                var referenceTypeId = context.GetModelNodeId(ReferenceTypeIds.HasComponent);
                if (GetOtherReferenceWithDerivedReferenceType(variable, referenceTypeId) == null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(referenceTypeId, context),
                        Value = GetNodeIdForExport(variable.NodeId, context),
                    });
                }
            }
            if (references.Any())
            {
                node.References = references.ToArray();
            }
            return (node, null, true);
        }

        protected string GetOtherReferenceWithDerivedReferenceType(NodeModel uaNode, string referenceTypeModelId)
        {
            return GetOtherReferenceWithDerivedReferenceType(_model, uaNode, referenceTypeModelId);
        }

        static protected string GetOtherReferenceWithDerivedReferenceType(NodeModel parentModel, NodeModel uaNode, string referenceTypeModelId)
        {
            var otherReferences = parentModel.OtherReferencedNodes.Where(nr => nr.Node == uaNode).ToList();
            var otherMatchingReference = otherReferences.FirstOrDefault(r => (r.ReferenceType as ReferenceTypeModel).SuperType == null || (r.ReferenceType as ReferenceTypeModel)?.HasBaseType(referenceTypeModelId) == true);
            if (otherMatchingReference != null && otherMatchingReference.ReferenceType.NodeId != referenceTypeModelId)
            {
                return otherMatchingReference.ReferenceType.NodeId;
            }
            return null;
        }

        protected string GetNodeIdForExport(NodeId nodeId, ExportContext context, bool applyAlias = true)
        {
            if (nodeId == null) return null;
            var nodeIdStr = nodeId.ToString();

            context._nodeIdsUsed?.Add(nodeIdStr);

            if (applyAlias && context.Aliases?.TryGetValue(nodeIdStr, out var alias) == true)
            {
                return alias;
            }
            return ExpandedNodeId.ToNodeId(nodeId, context.NamespaceUris).ToString();
        }
        protected string GetNodeIdForExport(string nodeId, ExportContext context, bool applyAlias = true)
        {
            if (nodeId == null) { return null; }
            NodeId parsedNodeId = GetNodeIdFromString(nodeId, context);
            return GetNodeIdForExport(parsedNodeId, context);
        }

        private NodeId GetNodeIdFromString(string nodeId, ExportContext context)
        {
            if (nodeId == null) return null;
            NodeId parsedNodeId;
            try
            {
                parsedNodeId = ExpandedNodeId.Parse(nodeId, context.NamespaceUris);
            }
            catch (ServiceResultException)
            {
                // try again after adding namespace to the namespace table
                var nameSpace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);
                context.NamespaceUris.GetIndexOrAppend(nameSpace);
                parsedNodeId = ExpandedNodeId.Parse(nodeId, context.NamespaceUris);
            }
            if (string.IsNullOrEmpty(context.NamespaceUris.GetString(parsedNodeId.NamespaceIndex)))
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid, "Namespace Uri for Node id ({0}) not specified or not found in the namespace table. Node Ids should be specified in nsu= format.", nodeId);
            }
            return parsedNodeId;
        }

        protected string GetBrowseNameForExport(NamespaceTable namespaces)
        {
            return GetQualifiedNameForExport(_model.BrowseName, _model.Namespace, _model.DisplayName, namespaces);
        }

        protected static string GetQualifiedNameForExport(string qualifiedName, string fallbackNamespace, List<NodeModel.LocalizedText> displayName, NamespaceTable namespaces)
        {
            string qualifiedNameForExport;
            if (qualifiedName != null)
            {
                var parts = qualifiedName.Split(new[] { ';' }, 2);
                if (parts.Length >= 2)
                {
                    qualifiedNameForExport = new QualifiedName(parts[1], namespaces.GetIndexOrAppend(parts[0])).ToString();
                }
                else if (parts.Length == 1)
                {
                    qualifiedNameForExport = parts[0];
                }
                else
                {
                    qualifiedNameForExport = "";
                }
            }
            else
            {
                qualifiedNameForExport = new QualifiedName(displayName?.FirstOrDefault()?.Text, namespaces.GetIndexOrAppend(fallbackNamespace)).ToString();
            }

            return qualifiedNameForExport;
        }

        public override string ToString()
        {
            return _model?.ToString();
        }
    }

    public abstract class InstanceModelExportOpc<TInstanceModel, TBaseTypeModel> : NodeModelExportOpc<TInstanceModel>
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : NodeModel, new()
    {

        protected abstract (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent);

        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<T>(context);
            if (!result.Created)
            {
                return result;
            }
            var instance = result.ExportedNode as UAInstance;
            if (instance == null)
            {
                throw new Exception("Internal error: wrong generic type requested");
            }
            var references = instance.References?.ToList() ?? new List<Reference>();

            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                instance.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, context);
            }

            string typeDefinitionNodeIdForExport;
            if (_model.TypeDefinition != null)
            {
                context.NamespaceUris.GetIndexOrAppend(_model.TypeDefinition.Namespace);
                typeDefinitionNodeIdForExport = GetNodeIdForExport(_model.TypeDefinition.NodeId, context);
            }
            else
            {
                NodeId typeDefinitionNodeId = null;
                if (_model is PropertyModel)
                {
                    typeDefinitionNodeId = VariableTypeIds.PropertyType;
                }
                else if (_model is DataVariableModel)
                {
                    typeDefinitionNodeId = VariableTypeIds.BaseDataVariableType;
                }
                else if (_model is VariableModel)
                {
                    typeDefinitionNodeId = VariableTypeIds.BaseVariableType;
                }
                else if (_model is ObjectModel)
                {
                    typeDefinitionNodeId = ObjectTypeIds.BaseObjectType;
                }

                typeDefinitionNodeIdForExport = GetNodeIdForExport(typeDefinitionNodeId, context);
            }
            if (typeDefinitionNodeIdForExport != null && !(_model.TypeDefinition is MethodModel))
            {
                var reference = new Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition, context),
                    Value = typeDefinitionNodeIdForExport,
                };
                references.Add(reference);
            }

            AddModellingRuleReference(_model.ModellingRule, references, context);

            if (references.Any())
            {
                instance.References = references.Distinct(new ReferenceComparer()).ToArray();
            }

            return (instance as T, result.AdditionalNodes, result.Created);
        }

        protected List<Reference> AddModellingRuleReference(string modellingRule, List<Reference> references, ExportContext context)
        {
            if (modellingRule != null)
            {
                var modellingRuleId = modellingRule switch
                {
                    "Optional" => ObjectIds.ModellingRule_Optional,
                    "Mandatory" => ObjectIds.ModellingRule_Mandatory,
                    "MandatoryPlaceholder" => ObjectIds.ModellingRule_MandatoryPlaceholder,
                    "OptionalPlaceholder" => ObjectIds.ModellingRule_OptionalPlaceholder,
                    "ExposesItsArray" => ObjectIds.ModellingRule_ExposesItsArray,
                    _ => null,
                };
                if (modellingRuleId != null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasModellingRule, context),
                        Value = GetNodeIdForExport(modellingRuleId, context),
                    });
                }
            }
            return references;
        }

        protected void AddOtherReferences(List<Reference> references, string parentNodeId, NodeId referenceTypeId, bool bIsChild, ExportContext context)
        {
            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                bool bAdded = false;
                foreach (var referencingNode in _model.Parent.OtherReferencedNodes.Where(cr => cr.Node == _model))
                {
                    var referenceType = GetNodeIdForExport(referencingNode.ReferenceType?.NodeId, context);
                    if (!references.Any(r => r.IsForward == false && r.Value == parentNodeId && r.ReferenceType != referenceType))
                    {
                        references.Add(new Reference { IsForward = false, ReferenceType = referenceType, Value = parentNodeId });
                    }
                    else
                    {
                        // TODO ensure we pick the most derived reference type
                    }
                    bAdded = true;
                }
                if (bIsChild || !bAdded)//_model.Parent.Objects.Contains(_model))
                {
                    var referenceType = GetNodeIdForExport(referenceTypeId, context);
                    if (!references.Any(r => r.IsForward == false && r.Value == parentNodeId && r.ReferenceType != referenceType))
                    {
                        references.Add(new Reference { IsForward = false, ReferenceType = referenceType, Value = parentNodeId });
                    }
                    else
                    {
                        // TODO ensure we pick the most derived reference type
                    }
                }
            }
        }



    }

    public class ObjectModelExportOpc : InstanceModelExportOpc<ObjectModel, ObjectTypeModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UAObject>(context);
            if (!result.Created)
            {
                return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
            }
            var uaObject = result.ExportedNode;
            if (_model.EventNotifier != null)
            {
                uaObject.EventNotifier = _model.EventNotifier.Value;
            }
            var references = uaObject.References?.ToList() ?? new List<Reference>();

            if (uaObject.ParentNodeId != null)
            {
                AddOtherReferences(references, uaObject.ParentNodeId, ReferenceTypeIds.HasComponent, _model.Parent.Objects.Contains(_model), context);
            }
            if (references.Any())
            {
                uaObject.References = references.Distinct(new ReferenceComparer()).ToArray();
            }

            return (uaObject as T, result.AdditionalNodes, result.Created);
        }

        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.Objects.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class BaseTypeModelExportOpc<TBaseTypeModel> : NodeModelExportOpc<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    //public class BaseTypeModelExportOpc<TBaseTypeModel> : NodeModelExportOpc<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel<TBaseTypeModel, TBaseTypeModel>, new()
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<T>(context);
            if (!result.Created)
            {
                return result;
            }
            var objectType = result.ExportedNode;
            foreach (var subType in this._model.SubTypes)
            {
                context.NamespaceUris.GetIndexOrAppend(subType.Namespace);
            }

            var superType = _model.SuperType;
            if (superType == null && _model.NodeId == context.GetModelNodeId(ObjectTypeIds.BaseInterfaceType))
            {
                superType = context.GetModelForNode<TBaseTypeModel>(_model.NodeId);
            }
            if (superType != null)
            {
                context.NamespaceUris.GetIndexOrAppend(superType.Namespace);
                var superTypeReference = new Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasSubtype, context),
                    IsForward = false,
                    Value = GetNodeIdForExport(superType.NodeId, context),
                };
                if (objectType.References == null)
                {
                    objectType.References = new Reference[] { superTypeReference };
                }
                else
                {
                    var referenceList = new List<Reference>(objectType.References);
                    referenceList.Add(superTypeReference);
                    objectType.References = referenceList.ToArray();
                }
            }
            if (objectType is UAType uaType)
            {
                uaType.IsAbstract = _model.IsAbstract;
            }
            else
            {
                throw new Exception("Must be UAType or derived");
            }
            return (objectType, result.AdditionalNodes, result.Created);
        }
    }

    public class ObjectTypeModelExportOpc<TTypeModel> : BaseTypeModelExportOpc<TTypeModel> where TTypeModel : BaseTypeModel, new()
    //public class ObjectTypeModelExportOpc<TTypeModel> : BaseTypeModelExportOpc<TTypeModel> where TTypeModel : BaseTypeModel<TTypeModel, TTypeModel>, new()
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UAObjectType>(context);
            var objectType = result.ExportedNode;
            return (objectType as T, result.AdditionalNodes, result.Created);
        }
    }

    public class ObjectTypeModelExportOpc : ObjectTypeModelExportOpc<ObjectTypeModel>
    {
    }

    public class InterfaceModelExportOpc : ObjectTypeModelExportOpc<InterfaceModel>
    {
    }

    public abstract class VariableModelExportOpc<TVariableModel> : InstanceModelExportOpc<TVariableModel, VariableTypeModel>
        where TVariableModel : VariableModel, new()
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            if (_model.DataType?.Namespace != null)
            {
                context.NamespaceUris.GetIndexOrAppend(_model.DataType.Namespace);
            }
            else
            {
                // TODO: should not happen - remove once coded
            }
            var result = base.GetUANode<UAVariable>(context);
            if (!result.Created)
            {
                return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
            }
            var dataVariable = result.ExportedNode;

            var references = dataVariable.References?.ToList() ?? new List<Reference>();

            if (!_model.Properties.Concat(_model.DataVariables).Any(p => p.NodeId == _model.EngUnitNodeId) && (_model.EngineeringUnit != null || !string.IsNullOrEmpty(_model.EngUnitNodeId)))
            {
                // Add engineering unit property
                if (result.AdditionalNodes == null)
                {
                    result.AdditionalNodes = new List<UANode>();
                }

                var engUnitProp = new UAVariable
                {
                    NodeId = GetNodeIdForExport(!String.IsNullOrEmpty(_model.EngUnitNodeId) ? _model.EngUnitNodeId : NodeModelOpcExtensions.GetNewNodeId(_model.Namespace), context),
                    BrowseName = BrowseNames.EngineeringUnits, // TODO preserve non-standard browsenames (detected based on data type)
                    DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = BrowseNames.EngineeringUnits } },
                    ParentNodeId = dataVariable.NodeId,
                    DataType = DataTypeIds.EUInformation.ToString(),
                    References = new Reference[]
                    {
                         new Reference {
                             ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition, context),
                             Value = GetNodeIdForExport(VariableTypeIds.PropertyType, context)
                         },
                         new Reference {
                             ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context),
                             IsForward = false,
                             Value = GetNodeIdForExport(dataVariable.NodeId, context),
                         },
                    },
                    AccessLevel = _model.EngUnitAccessLevel ?? 1,
                    // UserAccessLevel: deprecated: never emit
                };
                if (_model.EngUnitModellingRule != null)
                {
                    engUnitProp.References = AddModellingRuleReference(_model.EngUnitModellingRule, engUnitProp.References.ToList(), context).ToArray();
                }
                if (_model.EngineeringUnit != null)
                {
                    // Ensure EU type gets added to aliases
                    _ = GetNodeIdForExport(DataTypeIds.EUInformation, context);

                    EUInformation engUnits = NodeModelOpcExtensions.GetEUInformation(_model.EngineeringUnit);
                    var euXmlElement = NodeModelUtils.GetExtensionObjectAsXML(engUnits);
                    engUnitProp.Value = euXmlElement;
                }
                result.AdditionalNodes.Add(engUnitProp);
                references.Add(new Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context),
                    Value = engUnitProp.NodeId,
                });
            }

            AddRangeProperties(
                dataVariable.NodeId, _model.EURangeNodeId, BrowseNames.EURange, _model.EURangeAccessLevel, _model.EURangeModellingRule, _model.MinValue, _model.MaxValue,
                ref result.AdditionalNodes, references,
                context);

            AddRangeProperties(
                dataVariable.NodeId, _model.InstrumentRangeNodeId, BrowseNames.InstrumentRange, _model.InstrumentRangeAccessLevel, _model.InstrumentRangeModellingRule, _model.InstrumentMinValue, _model.InstrumentMaxValue,
                ref result.AdditionalNodes, references,
                context);

            if (_model.DataType != null)
            {
                dataVariable.DataType = GetNodeIdForExport(_model.DataType.NodeId, context);
            }
            dataVariable.ValueRank = _model.ValueRank ?? -1;
            dataVariable.ArrayDimensions = _model.ArrayDimensions;

            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                dataVariable.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, context);
                if (!references.Any(r => r.Value == dataVariable.ParentNodeId && r.IsForward == false))
                {
                    var referenceTypeNodeId = context.GetModelNodeId((_model.Parent.Properties.Contains(_model) ? ReferenceTypeIds.HasProperty : ReferenceTypeIds.HasComponent));
                    referenceTypeNodeId = GetOtherReferenceWithDerivedReferenceType(_model.Parent, _model, referenceTypeNodeId) ?? referenceTypeNodeId;
                    var reference = new Reference
                    {
                        IsForward = false,
                        ReferenceType = GetNodeIdForExport(referenceTypeNodeId, context),
                        Value = dataVariable.ParentNodeId
                    };
                    references.Add(reference);
                }
                else
                {
                    // TODO ensure we pick the most derived reference type
                }
            }
            if (_model.Value != null)
            {
                if (_model.DataType != null)
                {
                    ServiceMessageContext messageContext = NodeModelUtils.GetContextWithDynamicEncodeableFactory(_model.DataType, context.NamespaceUris);
                    dataVariable.Value = NodeModelUtils.JsonDecodeVariantToXml(_model.Value, messageContext, _model.DataType, context.EncodeJsonScalarsAsValue);
                }
                else
                {
                    // Unknown data type
                }
            }

            dataVariable.AccessLevel = _model.AccessLevel ?? 1;
            // deprecated: dataVariable.UserAccessLevel = _model.UserAccessLevel ?? 1;
            dataVariable.AccessRestrictions = (byte)(_model.AccessRestrictions ?? 0);
            dataVariable.UserWriteMask = _model.UserWriteMask ?? 0;
            dataVariable.WriteMask = _model.WriteMask ?? 0;
            dataVariable.MinimumSamplingInterval = _model.MinimumSamplingInterval ?? 0;

            if (references?.Any() == true)
            {
                dataVariable.References = references.ToArray();
            }
            return (dataVariable as T, result.AdditionalNodes, result.Created);
        }

        private void AddRangeProperties(
            string parentNodeId, string rangeNodeId, string rangeBrowseName, uint? rangeAccessLevel, string rangeModellingRule, double? minValue, double? maxValue, // inputs
            ref List<UANode> additionalNodes, List<Reference> references, // outputs
            ExportContext context) // lookups
        {
            if (!_model.Properties.Concat(_model.DataVariables).Any(p => p.NodeId == rangeNodeId) // if it's explicitly authored: don't auto-generate
                && (!string.IsNullOrEmpty(rangeNodeId) // if rangeNodeid or min/max are specified: do generate, otherwise skip
                    || (minValue.HasValue && maxValue.HasValue && minValue != maxValue)
                    ))
            {
                // Add EURange property
                if (additionalNodes == null)
                {
                    additionalNodes = new List<UANode>();
                }

                System.Xml.XmlElement xmlElem = null;

                if (minValue.HasValue && maxValue.HasValue)
                {
                    // Ensure EU type gets added to aliases
                    _ = GetNodeIdForExport(DataTypeIds.Range, context);
                    var range = new ua.Range
                    {
                        Low = minValue.Value,
                        High = maxValue.Value,
                    };
                    xmlElem = NodeModelUtils.GetExtensionObjectAsXML(range);
                }
                var euRangeProp = new UAVariable
                {
                    NodeId = GetNodeIdForExport(!String.IsNullOrEmpty(rangeNodeId) ? rangeNodeId : NodeModelOpcExtensions.GetNewNodeId(_model.Namespace), context),
                    BrowseName = rangeBrowseName,
                    DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = rangeBrowseName } },
                    ParentNodeId = parentNodeId,
                    DataType = GetNodeIdForExport(DataTypeIds.Range, context),
                    References = new[] {
                        new Reference {
                            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition, context),
                            Value = GetNodeIdForExport(VariableTypeIds.PropertyType, context),
                        },
                        new Reference
                        {
                            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context),
                            IsForward = false,
                            Value = GetNodeIdForExport(parentNodeId, context),
                        },
                    },
                    Value = xmlElem,
                    AccessLevel = rangeAccessLevel ?? 1,
                    // deprecated: UserAccessLevel = _model.EURangeUserAccessLevel ?? 1,
                };

                if (rangeModellingRule != null)
                {
                    euRangeProp.References = AddModellingRuleReference(rangeModellingRule, euRangeProp.References?.ToList() ?? new List<Reference>(), context).ToArray();
                }

                additionalNodes.Add(euRangeProp);
                references.Add(new Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context),
                    Value = GetNodeIdForExport(euRangeProp.NodeId, context),
                });
            }
        }
    }

    public class DataVariableModelExportOpc : VariableModelExportOpc<DataVariableModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<T>(context);
            var dataVariable = result.ExportedNode;
            //var references = dataVariable.References?.ToList() ?? new List<Reference>();
            //references.Add(new Reference { ReferenceType = "HasTypeDefinition", Value = GetNodeIdForExport(VariableTypeIds.BaseDataVariableType, context), });
            //dataVariable.References = references.ToArray();
            return (dataVariable, result.AdditionalNodes, result.Created);
        }

        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.DataVariables.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class PropertyModelExportOpc : VariableModelExportOpc<PropertyModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<T>(context);
            if (!result.Created)
            {
                return result;
            }
            var property = result.ExportedNode;
            var references = property.References?.ToList() ?? new List<Reference>();
            var propertyTypeNodeId = GetNodeIdForExport(VariableTypeIds.PropertyType, context);
            if (references?.Any(r => r.Value == propertyTypeNodeId) == false)
            {
                references.Add(new Reference { ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition, context), Value = propertyTypeNodeId, });
            }
            property.References = references.ToArray();
            return (property, result.AdditionalNodes, result.Created);
        }
        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (false, ReferenceTypeIds.HasProperty);
        }
    }

    public class MethodModelExportOpc : InstanceModelExportOpc<MethodModel, MethodModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UAMethod>(context);
            if (!result.Created)
            {
                return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
            }
            var method = result.ExportedNode;
            method.MethodDeclarationId = GetNodeIdForExport(_model.TypeDefinition?.NodeId, context);
            // method.ArgumentDescription = null; // TODO - not commonly used
            if (method.ParentNodeId != null)
            {
                var references = method.References?.ToList() ?? new List<Reference>();
                AddOtherReferences(references, method.ParentNodeId, ReferenceTypeIds.HasComponent, _model.Parent.Methods.Contains(_model), context);
                method.References = references.Distinct(new ReferenceComparer()).ToArray();
            }
            return (method as T, result.AdditionalNodes, result.Created);
        }

        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.Methods.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class VariableTypeModelExportOpc : BaseTypeModelExportOpc<VariableTypeModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UAVariableType>(context);
            if (!result.Created)
            {
                return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
            }
            var variableType = result.ExportedNode;
            variableType.IsAbstract = _model.IsAbstract;
            if (_model.DataType != null)
            {
                variableType.DataType = GetNodeIdForExport(_model.DataType.NodeId, context);
            }
            if (_model.ValueRank != null)
            {
                variableType.ValueRank = _model.ValueRank.Value;
            }
            variableType.ArrayDimensions = _model.ArrayDimensions;
            if (_model.Value != null)
            {
                ServiceMessageContext messageContext = NodeModelUtils.GetContextWithDynamicEncodeableFactory(_model.DataType, context.NamespaceUris);
                variableType.Value = NodeModelUtils.JsonDecodeVariantToXml(_model.Value, messageContext, _model.DataType, true); // TODO make this configurable by callers);
            }
            return (variableType as T, result.AdditionalNodes, result.Created);
        }
    }
    public class DataTypeModelExportOpc : BaseTypeModelExportOpc<DataTypeModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UADataType>(context);
            if (!result.Created)
            {
                return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
            }
            var dataType = result.ExportedNode;
            if (_model.StructureFields?.Any() == true)
            {
                var fields = new List<DataTypeField>();
                foreach (var field in _model.StructureFields.OrderBy(f => f.FieldOrder))
                {
                    var uaField = new DataTypeField
                    {
                        Name = field.Name,
                        SymbolicName = field.SymbolicName,
                        DataType = GetNodeIdForExport(field.DataType.NodeId, context),
                        Description = field.Description.ToExport().ToArray(),
                        ArrayDimensions = field.ArrayDimensions,
                        IsOptional = field.IsOptional,
                        AllowSubTypes = field.AllowSubTypes,
                    };
                    if (field.ValueRank != null)
                    {
                        uaField.ValueRank = field.ValueRank.Value;
                    }
                    if (field.MaxStringLength != null)
                    {
                        uaField.MaxStringLength = field.MaxStringLength.Value;
                    }
                    fields.Add(uaField);
                }
                dataType.Definition = new uaExport.DataTypeDefinition
                {
                    Name = GetBrowseNameForExport(context.NamespaceUris),
                    SymbolicName = _model.SymbolicName,
                    Field = fields.ToArray(),
                };
            }
            if (_model.EnumFields?.Any() == true)
            {
                var enumValues = new List<EnumValueType>();
                var fields = new List<DataTypeField>();

                // Some nodesets use an improper browsename in their own namespace: tolerate this on export
                var existingEnumStringOrValuesModel = _model.Properties.FirstOrDefault(p =>
                        p.BrowseName.EndsWith(BrowseNames.EnumValues)
                        || p.BrowseName.EndsWith(BrowseNames.EnumStrings)
                        || p.BrowseName.EndsWith(BrowseNames.OptionSetValues)
                        );
                int i = 0;
                bool requiresEnumValues = false;
                bool hasDescription = false;
                long previousValue = -1;
                foreach (var field in _model.EnumFields.OrderBy(f => f.Value))
                {
                    var dtField = new DataTypeField
                    {
                        Name = field.Name,
                        DisplayName = field.DisplayName?.ToExport().ToArray(),
                        Description = field.Description?.ToExport().ToArray(),
                        Value = (int)field.Value,
                        SymbolicName = field.SymbolicName,
                        // TODO: 
                        //DataType = field.DataType,                         
                    };
                    fields.Add(dtField);
                    if (_model.IsOptionSet == true && previousValue + 1 < field.Value)
                    {
                        var reserved = new EnumValueType { DisplayName = new ua.LocalizedText("Reserved"), };
                        for (long j = previousValue + 1; j < field.Value; j++)
                        {
                            enumValues.Add(reserved);
                        }
                    }
                    enumValues.Add(new EnumValueType
                    {
                        DisplayName = new ua.LocalizedText(field.DisplayName?.FirstOrDefault()?.Text ?? field.Name),
                        Description = new ua.LocalizedText(field.Description?.FirstOrDefault()?.Text),
                        Value = field.Value,
                    });
                    if (field.Value != i)
                    {
                        // Non-consecutive,non-zero based values require EnumValues instead of EnumStrings. Also better for capturing displayname and description if provided.
                        requiresEnumValues = true;
                    }
                    if (field.DisplayName?.Any() == true || field.Description?.Any() == true)
                    {
                        hasDescription = true;
                    }
                    i++;
                    previousValue = field.Value;
                }
                if (_model.IsOptionSet == true)
                {
                    requiresEnumValues = false;
                }
                else if (existingEnumStringOrValuesModel?.BrowseName?.EndsWith(BrowseNames.EnumValues) == true)
                {
                    // Keep as authored even if not technically required
                    requiresEnumValues = true;
                }
                else if (existingEnumStringOrValuesModel == null)
                {
                    // Only switch to enum values due to description if no authored node
                    requiresEnumValues |= hasDescription;
                }
                dataType.Definition = new uaExport.DataTypeDefinition
                {
                    Name = GetBrowseNameForExport(context.NamespaceUris),
                    Field = fields.ToArray(),
                };
                string browseName;
                XmlElement enumValuesXml;
                if (requiresEnumValues)
                {
                    enumValuesXml = NodeModelUtils.EncodeAsXML((e) =>
                        {
                            e.PushNamespace(Namespaces.OpcUaXsd);
                            e.WriteExtensionObjectArray("ListOfExtensionObject", new ExtensionObjectCollection(enumValues.Select(ev => new ExtensionObject(ev))));
                            e.PopNamespace();
                        }).FirstChild as XmlElement;
                    browseName = BrowseNames.EnumValues;
                }
                else
                {
                    enumValuesXml = NodeModelUtils.EncodeAsXML((e) =>
                        {
                            e.PushNamespace(Namespaces.OpcUaXsd);
                            e.WriteLocalizedTextArray("ListOfLocalizedText", enumValues.Select(ev => ev.DisplayName).ToArray());
                            e.PopNamespace();
                        }).FirstChild as XmlElement;
                    browseName = _model.IsOptionSet == true ? BrowseNames.OptionSetValues : BrowseNames.EnumStrings;
                }

                string enumValuesNodeId;
                string hasPropertyReferenceTypeId = GetNodeIdForExport(ReferenceTypeIds.HasProperty, context);
                UAVariable enumValuesProp;
                if (result.AdditionalNodes == null)
                {
                    result.AdditionalNodes = new List<UANode>();
                }
                if (existingEnumStringOrValuesModel != null)
                {
                    enumValuesNodeId = GetNodeIdForExport(existingEnumStringOrValuesModel.NodeId, context);
                    dataType.References = dataType.References?.Where(r => r.ReferenceType != hasPropertyReferenceTypeId && r.Value != enumValuesNodeId)?.ToArray();
                    var enumPropResult = NodeModelExportOpc.GetUANode(existingEnumStringOrValuesModel, context);
                    if (enumPropResult.AdditionalNodes != null)
                    {
                        result.AdditionalNodes.AddRange(enumPropResult.AdditionalNodes);
                    }
                    enumValuesProp = enumPropResult.ExportedNode as UAVariable;
                    enumValuesProp.BrowseName = browseName;
                    enumValuesProp.DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = browseName } };
                    enumValuesProp.Value = enumValuesXml;
                    enumValuesProp.DataType = requiresEnumValues ? DataTypeIds.EnumValueType.ToString() : DataTypeIds.LocalizedText.ToString();
                    enumValuesProp.ValueRank = 1;
                    enumValuesProp.ArrayDimensions = enumValues.Count.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    enumValuesNodeId = GetNodeIdForExport(NodeModelOpcExtensions.GetNewNodeId(_model.Namespace), context);
                    enumValuesProp = new uaExport.UAVariable
                    {
                        NodeId = enumValuesNodeId,
                        BrowseName = browseName,
                        DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = browseName } },
                        ParentNodeId = result.ExportedNode.NodeId,
                        DataType = requiresEnumValues ? DataTypeIds.EnumValueType.ToString() : DataTypeIds.LocalizedText.ToString(),
                        ValueRank = 1,
                        ArrayDimensions = enumValues.Count.ToString(CultureInfo.InvariantCulture),
                        References = new Reference[]
                        {
                             new Reference {
                                 ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition, context),
                                 Value = GetNodeIdForExport(VariableTypeIds.PropertyType, context)
                             },
                        },
                        Value = enumValuesXml,
                    };
                }
                var dtReferences = dataType.References?.ToList();
                dtReferences.Add(new Reference
                {
                    ReferenceType = hasPropertyReferenceTypeId,
                    Value = enumValuesProp.NodeId,
                });
                dataType.References = dtReferences.ToArray();
                result.AdditionalNodes.Add(enumValuesProp);
            }
            if (_model.IsOptionSet != null)
            {
                if (dataType.Definition == null)
                {
                    dataType.Definition = new uaExport.DataTypeDefinition { };
                }
                dataType.Definition.IsOptionSet = _model.IsOptionSet.Value;
            }
            return (dataType as T, result.AdditionalNodes, result.Created);
        }
    }

    public class ReferenceTypeModelExportOpc : BaseTypeModelExportOpc<ReferenceTypeModel>
    {
        public override (T ExportedNode, List<UANode> AdditionalNodes, bool Created) GetUANode<T>(ExportContext context)
        {
            var result = base.GetUANode<UAReferenceType>(context);
            result.ExportedNode.IsAbstract = _model.IsAbstract;
            result.ExportedNode.InverseName = _model.InverseName?.ToExport().ToArray();
            result.ExportedNode.Symmetric = _model.Symmetric;
            return (result.ExportedNode as T, result.AdditionalNodes, result.Created);
        }
    }

    public static class LocalizedTextExtension
    {
        public static uaExport.LocalizedText ToExport(this NodeModel.LocalizedText localizedText) => localizedText?.Text != null || localizedText?.Locale != null ? new uaExport.LocalizedText { Locale = localizedText.Locale, Value = localizedText.Text } : null;
        public static IEnumerable<uaExport.LocalizedText> ToExport(this IEnumerable<NodeModel.LocalizedText> localizedTexts) => localizedTexts?.Select(d => d.Text != null || d.Locale != null ? new uaExport.LocalizedText { Locale = d.Locale, Value = d.Text } : null).ToArray();
    }

}