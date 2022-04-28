using Opc.Ua;
using ua = Opc.Ua;
using uaExport = Opc.Ua.Export;

using System;
using System.Collections.Generic;
using System.Linq;

using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;

namespace CESMII.OpcUa.NodeSetModel.Export.Opc
{
    public class NodeModelExportOpc : NodeModelExportOpc<NodeModel>
    {

    }
    public class NodeModelExportOpc<T> where T : NodeModel, new()
    {
        public T _model;

        public static (uaExport.UANode, List<uaExport.UANode>) GetUANode(NodeModel model, NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (model is InterfaceModel uaInterface)
            {
                return new InterfaceModelExportOpc { _model = uaInterface }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is ObjectTypeModel objectType)
            {
                return new ObjectTypeModelExportOpc { _model = objectType }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is VariableTypeModel variableType)
            {
                return new VariableTypeModelExportOpc { _model = variableType }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is DataTypeModel dataType)
            {
                return new DataTypeModelExportOpc { _model = dataType }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is DataVariableModel dataVariable)
            {
                return new DataVariableModelExportOpc { _model = dataVariable }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is PropertyModel property)
            {
                return new PropertyModelExportOpc { _model = property }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is VariableModel variable)
            {
                return new VariableModelExportOpc<VariableModel> { _model = variable}.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is ObjectModel uaObject)
            {
                return new ObjectModelExportOpc { _model = uaObject }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            else if (model is MethodModel uaMethod)
            {
                return new MethodModelExportOpc { _model = uaMethod }.GetUANode<uaExport.UANode>(namespaces, aliases);
            }
            throw new Exception($"Unexpected node model {model.GetType()}");
        }

        public virtual (TUANode, List<uaExport.UANode>) GetUANode<TUANode>(NamespaceTable namespaces, Dictionary<string, string> aliases) where TUANode : uaExport.UANode, new()
        {
            var node = new TUANode
            {
                Description = _model.Description?.ToExport()?.ToArray(),
                BrowseName = GetBrowseNameForExport(namespaces),
                SymbolicName = _model.SymbolicName,
                DisplayName = _model.DisplayName?.ToExport()?.ToArray(),
                NodeId = GetNodeIdForExport(_model.NodeId, namespaces, aliases),
                Documentation = _model.Documentation,
                Category = _model.Categories?.ToArray(),
            };
            var references = new List<uaExport.Reference>();
            foreach (var property in _model.Properties)
            {
                namespaces.GetIndexOrAppend(property.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(property.NodeId, namespaces, aliases),
                });
            }
            foreach (var uaObject in this._model.Objects)
            {
                namespaces.GetIndexOrAppend(uaObject.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasComponent.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(uaObject.NodeId, namespaces, aliases),
                });
            }
            foreach (var childRef in this._model.OtherChilden)
            {
                namespaces.GetIndexOrAppend(childRef.Child.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(childRef.Reference, namespaces, aliases),
                    Value = GetNodeIdForExport(childRef.Child.NodeId, namespaces, aliases),
                });
            }
            foreach (var uaInterface in this._model.Interfaces)
            {
                namespaces.GetIndexOrAppend(uaInterface.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasInterface.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(uaInterface.NodeId, namespaces, aliases),
                });

            }
            foreach (var method in this._model.Methods)
            {
                namespaces.GetIndexOrAppend(method.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasComponent.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(method.NodeId, namespaces, aliases),
                });

            }
            foreach (var uaEvent in this._model.Events)
            {
                namespaces.GetIndexOrAppend(uaEvent.Namespace);
                references.Add(new uaExport.Reference 
                { 
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.GeneratesEvent.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(uaEvent.NodeId, namespaces, aliases),
                });
            }
            foreach (var variable in this._model.DataVariables)
            {
                namespaces.GetIndexOrAppend(variable.Namespace);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasComponent.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(variable.NodeId, namespaces, aliases),
                });
            }
            if (references.Any())
            {
                node.References = references.ToArray();
            }
            return (node, null);
        }

        protected static string GetNodeIdForExport(string nodeId, NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (nodeId == null) return null;
            var expandedNodeId = ExpandedNodeId.Parse(nodeId, namespaces);
            if (expandedNodeId.NamespaceIndex == 0 && aliases.TryGetValue(expandedNodeId.ToString(), out var alias))
            {
                return alias;
            }
            return ExpandedNodeId.ToNodeId(expandedNodeId, namespaces).ToString();
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
    }

    public class InstanceModelExportOpc<TInstanceModel, TBaseTypeModel, TBaseTypeModelExportOpc> : NodeModelExportOpc<TInstanceModel> 
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new() 
        where TBaseTypeModel : BaseTypeModel, new()
        where TBaseTypeModelExportOpc : NodeModelExportOpc<TBaseTypeModel>, new()
    {

        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<T>(namespaces, aliases);
            var instance = result.Item1;
            var references = instance.References?.ToList() ?? new List<uaExport.Reference>();

            string typeDefinitionNodeIdForExport;
            if (_model.TypeDefinition != null)
            {
                namespaces.GetIndexOrAppend(_model.TypeDefinition.Namespace);
                typeDefinitionNodeIdForExport = GetNodeIdForExport(_model.TypeDefinition.NodeId, namespaces, aliases);
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
                else
                {
                }

                typeDefinitionNodeIdForExport = GetNodeIdForExport(typeDefinitionNodeId?.ToString(), namespaces, aliases);
            }
            if (typeDefinitionNodeIdForExport != null)
            {
                var reference = new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases),
                    Value = typeDefinitionNodeIdForExport,
                };
                references.Add(reference);
            }
            if (_model.ModelingRule != null)
            {
                var modelingRuleId = _model.ModelingRule switch
                {
                    "Optional" => ObjectIds.ModellingRule_Optional,
                    "Mandatory" => ObjectIds.ModellingRule_Mandatory,
                    "MandatoryPlaceholder" => ObjectIds.ModellingRule_MandatoryPlaceholder,
                    "OptionalPlaceholder" => ObjectIds.ModellingRule_OptionalPlaceholder,
                    "ExposesItsArray" => ObjectIds.ModellingRule_ExposesItsArray,
                    _ => null,
                };
                if (modelingRuleId != null)
                {
                    references.Add(new uaExport.Reference
                    {
                        ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasModellingRule.ToString(), namespaces, aliases),
                        Value = GetNodeIdForExport(modelingRuleId.ToString(), namespaces, aliases),
                    });
                }
            }
            instance.References = references.ToArray();
            return result;
        }
    }

    public class ObjectModelExportOpc : InstanceModelExportOpc<ObjectModel, ObjectTypeModel, ObjectTypeModelExportOpc>
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<uaExport.UAObject>(namespaces, aliases);
            var uaObject = result.Item1;

            var references = uaObject.References?.ToList() ?? new List<uaExport.Reference>();

            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                uaObject.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, namespaces, aliases);
                bool bAdded = false;
                foreach (var reference in _model.Parent.OtherChilden.Where(cr => cr.Child == _model))
                {
                    references.Add(new uaExport.Reference { IsForward = false, ReferenceType = GetNodeIdForExport( reference.Reference, namespaces, aliases), Value = uaObject.ParentNodeId });
                    bAdded = true;
                }
                if (_model.Parent.Objects.Contains(_model))
                {
                    bAdded = true;
                    references.Add(new uaExport.Reference { IsForward = false, ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasComponent.ToString(), namespaces, aliases), Value = uaObject.ParentNodeId });
                }
                else
                {
                    if (!bAdded)
                    {
                        references.Add(new uaExport.Reference { IsForward = false, ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasComponent.ToString(), namespaces, aliases), Value = uaObject.ParentNodeId });
                    }
                }
            }
            if (references.Any())
            {
                uaObject.References = references.ToArray();
            }

            return (uaObject as T, result.Item2);
        }
    }

    public class BaseTypeModelExportOpc<TBaseTypeModel> : NodeModelExportOpc<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<T>(namespaces, aliases);
            var objectType = result.Item1;
            foreach (var subType in this._model.SubTypes)
            {
                namespaces.GetIndexOrAppend(subType.Namespace);
            }
            if (_model.SuperType != null)
            {
                namespaces.GetIndexOrAppend(_model.SuperType.Namespace);
                var superTypeReference = new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasSubtype.ToString(), namespaces, aliases),
                    IsForward = false,
                    Value = GetNodeIdForExport(_model.SuperType.NodeId, namespaces, aliases),
                };
                if (objectType.References == null)
                {
                    objectType.References = new uaExport.Reference[] { superTypeReference };
                }
                else
                {
                    var referenceList = new List<uaExport.Reference>(objectType.References);
                    referenceList.Add(superTypeReference);
                    objectType.References = referenceList.ToArray();
                }
            }
            if (objectType is uaExport.UAType uaType)
            {
                uaType.IsAbstract = _model.IsAbstract;
            }
            else
            {
                throw new Exception("Must be UAType or derived");
            }
            return (objectType as T, result.Item2);
        }
    }

    public class ObjectTypeModelExportOpc<TTypeModel> : BaseTypeModelExportOpc<TTypeModel> where TTypeModel : ObjectTypeModel, new()
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<uaExport.UAObjectType>(namespaces, aliases);
            var objectType = result.Item1;
            return (objectType as T, result.Item2);
        }
    }

    public class ObjectTypeModelExportOpc : ObjectTypeModelExportOpc<ObjectTypeModel>
    {
    }

    public class InterfaceModelExportOpc : ObjectTypeModelExportOpc<InterfaceModel>
    {
    }

    public class VariableModelExportOpc<TVariableModel> : InstanceModelExportOpc<TVariableModel, VariableTypeModel, VariableTypeModelExportOpc>
        where TVariableModel : VariableModel, new()
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (_model.DataType?.Namespace != null)
            {
                namespaces.GetIndexOrAppend(_model.DataType.Namespace);
            }
            else
            {
                // TODO: should not happen - remove once coded
            }
            var result = base.GetUANode<uaExport.UAVariable>(namespaces, aliases);
            var dataVariable = result.Item1;

            var references = dataVariable.References?.ToList() ?? new List<uaExport.Reference>();

            if (_model.EngineeringUnit != null || !string.IsNullOrEmpty(_model.EngUnitNodeId))
            {
                // Add engineering unit property
                if (result.Item2 == null)
                {
                    result.Item2 = new List<uaExport.UANode>();
                }

                var engUnitProp = new uaExport.UAVariable
                {
                    NodeId = GetNodeIdForExport(!String.IsNullOrEmpty(_model.EngUnitNodeId) ? _model.EngUnitNodeId : GetNodeIdForExport($"nsu={_model.Namespace};g={Guid.NewGuid()}", namespaces, aliases), namespaces, aliases),
                    BrowseName = BrowseNames.EngineeringUnits,
                    DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = BrowseNames.EngineeringUnits } },
                    ParentNodeId = dataVariable.NodeId,
                    DataType = DataTypeIds.EUInformation.ToString(),
                    References = new uaExport.Reference[]
                    {
                         new uaExport.Reference { 
                             ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases),
                             Value = GetNodeIdForExport(VariableTypeIds.PropertyType.ToString(), namespaces, aliases)
                         },
                         new uaExport.Reference {
                             ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasModellingRule.ToString(), namespaces, aliases),
                             Value = GetNodeIdForExport(ObjectIds.ModellingRule_Mandatory.ToString(), namespaces, aliases),
                         }, // TODO Does this need to be preserved?
                         new uaExport.Reference {
                             ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
                             IsForward = false, 
                             Value = GetNodeIdForExport(dataVariable.NodeId, namespaces, aliases),
                         },

                    }
                };
                if (_model.EngineeringUnit != null)
                {
                    EUInformation engUnits = NodeModelOpcExtensions.GetEUInformation(_model.EngineeringUnit);
                    var euXmlElement = GetExtensionObjectAsXML(engUnits);
                    engUnitProp.Value = euXmlElement;
                }
                result.Item2.Add(engUnitProp);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
                    Value = engUnitProp.NodeId,
                });
            }
            if (_model.MinValue.HasValue && _model.MaxValue.HasValue)
            {
                // Add EURange property
                if (result.Item2 == null)
                {
                    result.Item2 = new List<uaExport.UANode>();
                }


                var range = new ua.Range
                {
                    Low = _model.MinValue.Value,
                    High = _model.MaxValue.Value,
                };
                System.Xml.XmlElement xmlElem = GetExtensionObjectAsXML(range);

                var euRangeProp = new uaExport.UAVariable
                {
                    NodeId = GetNodeIdForExport($"nsu={_model.Namespace};g={Guid.NewGuid()}", namespaces, aliases), // TODO How do we preserve EURange NodeId?
                    BrowseName = BrowseNames.EURange,
                    DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = BrowseNames.EURange } },

                    Value = xmlElem,
                };

                result.Item2.Add(euRangeProp);
                references.Add(new uaExport.Reference
                {
                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
                    Value = GetNodeIdForExport(euRangeProp.NodeId, namespaces, aliases),
                });
            }
            if (_model.DataType != null)
            {
                dataVariable.DataType = GetNodeIdForExport(_model.DataType.NodeId, namespaces, aliases);
            }
            dataVariable.ValueRank = _model.ValueRank??-1;
            dataVariable.ArrayDimensions = _model.ArrayDimensions;

            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                dataVariable.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, namespaces, aliases);
                var reference = new uaExport.Reference
                {
                    IsForward = false,
                    ReferenceType = GetNodeIdForExport((_model is PropertyModel ? ReferenceTypeIds.HasProperty : ReferenceTypeIds.HasComponent).ToString(), namespaces, aliases),
                    Value = dataVariable.ParentNodeId
                };
                references.Add(reference);
            }
            if (_model.Value != null)
            {
                using (var decoder = new JsonDecoder(_model.Value, ServiceMessageContext.GlobalContext))
                {
                    var value = decoder.ReadVariant("Value");
                    var xml = GetVariantAsXML(value);
                    dataVariable.Value = xml;
                }
            }

            dataVariable.AccessLevel = _model.AccessLevel ?? 1;
            dataVariable.UserAccessLevel = _model.UserAccessLevel ?? 1;
            dataVariable.AccessRestrictions = (byte) (_model.AccessRestrictions ?? 0);
            dataVariable.UserWriteMask = _model.UserWriteMask ?? 0;
            dataVariable.WriteMask = _model.WriteMask ?? 0;

            if (references?.Any() == true)
            {
                dataVariable.References = references.ToArray();
            }
            return (dataVariable as T, result.Item2);
        }

        private static System.Xml.XmlElement GetExtensionObjectAsXML(object extensionBody)
        {
            var extension = new ExtensionObject(extensionBody);
            var context = new ServiceMessageContext();
            var ms = new System.IO.MemoryStream();
            using (var xmlWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8))
            {
                xmlWriter.WriteStartDocument();

                using (var encoder = new XmlEncoder(new System.Xml.XmlQualifiedName("uax:ExtensionObject", null), xmlWriter, context))
                {
                    encoder.WriteExtensionObject(null, extension);
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
            }
            var xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml.Substring(1));
            var xmlElem = doc.DocumentElement;
            return xmlElem;
        }
        private static System.Xml.XmlElement GetVariantAsXML(Variant value)
        {
            var context = new ServiceMessageContext();
            var ms = new System.IO.MemoryStream();
            using (var xmlWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8))
            {
                xmlWriter.WriteStartDocument();
                using (var encoder = new XmlEncoder(new System.Xml.XmlQualifiedName("myRoot"/*, "http://opcfoundation.org/UA/2008/02/Types.xsd"*/), xmlWriter, context))
                {
                    encoder.WriteVariant("value", value);
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
            }
            var xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            var doc = new System.Xml.XmlDocument();

            doc.LoadXml(xml.Substring(1));
            var xmlElem = doc.DocumentElement;
            var xmlValue = xmlElem.FirstChild?.FirstChild?.FirstChild as System.Xml.XmlElement;
            return xmlValue;
        }
    }

    public class DataVariableModelExportOpc : VariableModelExportOpc<DataVariableModel>
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<T>(namespaces, aliases);
            var dataVariable = result.Item1;
            //var references = dataVariable.References?.ToList() ?? new List<uaExport.Reference>();
            //references.Add(new uaExport.Reference { ReferenceType = "HasTypeDefinition", Value = GetNodeIdForExport(VariableTypeIds.BaseDataVariableType.ToString(), namespaces, aliases), });
            //dataVariable.References = references.ToArray();
            return (dataVariable, result.Item2);
        }
    }

    public class PropertyModelExportOpc : VariableModelExportOpc<PropertyModel>
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result =  base.GetUANode<T>(namespaces, aliases);
            var property = result.Item1;
            var references = property.References?.ToList() ?? new List<uaExport.Reference>();
            var propertyTypeNodeId = GetNodeIdForExport(VariableTypeIds.PropertyType.ToString(), namespaces, aliases);
            if (references?.Any(r => r.Value == propertyTypeNodeId) == false)
            {
                references.Add(new uaExport.Reference { ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases), Value = propertyTypeNodeId, });
            }
            property.References = references.ToArray();
            return (property, result.Item2);
        }
    }

    public class MethodModelExportOpc : NodeModelExportOpc<MethodModel> // TODO determine if intermediate base classes of MethodState are worth exposing in the model
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<uaExport.UAMethod>(namespaces, aliases);
            var method = result.Item1;
            method.MethodDeclarationId = GetNodeIdForExport(_model.TypeDefinition?.NodeId, namespaces, aliases);
            // method.ArgumentDescription = null; // TODO - not commonly used

            return (method as T, result.Item2);
        }
    }

    public class VariableTypeModelExportOpc : BaseTypeModelExportOpc<VariableTypeModel>
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<uaExport.UAVariableType>(namespaces, aliases);
            var variableType = result.Item1;
            variableType.IsAbstract = _model.IsAbstract;
            return (variableType as T, result.Item2);
        }
    }
    public class DataTypeModelExportOpc : BaseTypeModelExportOpc<DataTypeModel>
    {
        public override (T, List<uaExport.UANode>) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            var result = base.GetUANode<uaExport.UADataType>(namespaces, aliases);
            var dataType = result.Item1;
            if (_model.StructureFields?.Any() == true)
            {
                var fields = new List<uaExport.DataTypeField>();
                foreach(var field in _model.StructureFields)
                {
                    fields.Add(new uaExport.DataTypeField
                    {
                        Name = field.Name,
                        DataType = GetNodeIdForExport(field.DataType.NodeId, namespaces, aliases),
                        Description = field.Description.ToExport().ToArray(),
                        IsOptional = field.IsOptional,
                    });
                }
                dataType.Definition = new uaExport.DataTypeDefinition 
                { 
                     Name = GetBrowseNameForExport(namespaces),
                     Field = fields.ToArray(),
                };
            }
            if (_model.EnumFields?.Any() == true)
            {
                var fields = new List<uaExport.DataTypeField>();
                foreach (var field in _model.EnumFields)
                {
                    fields.Add(new uaExport.DataTypeField
                    {
                        Name = field.Name,
                        DisplayName = field.DisplayName?.ToExport().ToArray(),
                        Description = field.Description?.ToExport().ToArray(),
                        Value = (int) field.Value,
                    });
                }
                dataType.Definition = new uaExport.DataTypeDefinition
                {
                    Name = GetBrowseNameForExport(namespaces),
                    Field = fields.ToArray(),
                };
            }
            return (dataType as T, result.Item2);
        }

    }

    public static class LocalizedTextExtension
    {
        public static uaExport.LocalizedText ToExport(this NodeModel.LocalizedText localizedText) => localizedText?.Text != null || localizedText?.Locale != null ? new uaExport.LocalizedText { Locale = localizedText.Locale, Value = localizedText.Text } : null;
        public static IEnumerable<uaExport.LocalizedText> ToExport(this IEnumerable<NodeModel.LocalizedText> localizedTexts) => localizedTexts?.Select(d => d.Text != null || d.Locale != null ? new uaExport.LocalizedText { Locale = d.Locale, Value = d.Text } : null).ToArray();
    }

}