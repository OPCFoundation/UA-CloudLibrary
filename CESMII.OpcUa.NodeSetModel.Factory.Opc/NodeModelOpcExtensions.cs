using Opc.Ua;
using ua = Opc.Ua;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NotVisualBasic.FileIO;
using System.Reflection;
using Opc.Ua.Export;
using System;

using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace CESMII.OpcUa.NodeSetModel.Opc.Extensions
{
    public static class NodeModelOpcExtensions
    {
        public static string GetDisplayNamePath(this InstanceModelBase model)
        {
            return model.GetDisplayNamePath(new List<NodeModel>());
        }
        public static DateTime GetNormalizedPublicationDate(this ModelTableEntry model)
        {
            return model.PublicationDateSpecified ? DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc) : default;
        }
        public static DateTime GetNormalizedPublicationDate(this DateTime? publicationDate)
        {
            return publicationDate != null ? DateTime.SpecifyKind(publicationDate.Value, DateTimeKind.Utc) : default;
        }
        public static string GetDisplayNamePath(this InstanceModelBase model, List<NodeModel> nodesVisited)
        {
            if (nodesVisited.Contains(model))
            {
                return "(cycle)";
            }
            nodesVisited.Add(model);
            if (model.Parent is InstanceModelBase parent)
            {
                return $"{parent.GetDisplayNamePath(nodesVisited)}.{model.DisplayName.FirstOrDefault()?.Text}";
            }
            return model.DisplayName.FirstOrDefault()?.Text;
        }
        public static string GetUnqualifiedBrowseName(this NodeModel _this)
        {
            var browseName = _this.GetBrowseName();
            var parts = browseName.Split(new[] { ';' }, 2);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return browseName;
        }

        public enum JsonValueType
        {
            /// <summary>
            /// JSON object
            /// </summary>
            Object,
            /// <summary>
            /// Scalar, to be quoted
            /// </summary>
            String,
            /// <summary>
            /// Scalar, not to be quoted
            /// </summary>
            Value
        }

        public static bool IsJsonScalar(this DataTypeModel dataType)
        {
            return GetJsonValueType(dataType) != JsonValueType.Object;
        }
        public static JsonValueType GetJsonValueType(this DataTypeModel dataType)
        {
            if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.String}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Int64}") // numeric, but encoded as string
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.UInt64}") // numeric, but encoded as string
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.DateTime}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.ByteString}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.String}")
                )
            {
                return JsonValueType.String;
            }
            if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Boolean}")
                || (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Number}")
                    && !dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Decimal}") // numeric, but encoded as Scale/Value object
                    )
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.StatusCode}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Enumeration}")
                )
            {
                return JsonValueType.Value;
            }
            return JsonValueType.Object;
        }

        internal static void SetEngineeringUnits(this VariableModel model, EUInformation euInfo)
        {
            model.EngineeringUnit = new VariableModel.EngineeringUnitInfo
            {
                DisplayName = euInfo.DisplayName?.ToModelSingle(),
                Description = euInfo.Description?.ToModelSingle(),
                NamespaceUri = euInfo.NamespaceUri,
                UnitId = euInfo.UnitId,
            };
        }

        internal static void SetRange(this VariableModel model, ua.Range euRange)
        {
            model.MinValue = euRange.Low;
            model.MaxValue = euRange.High;
        }
        internal static void SetInstrumentRange(this VariableModel model, ua.Range range)
        {
            model.InstrumentMinValue = range.Low;
            model.InstrumentMaxValue = range.High;
        }

        private const string strUNECEUri = "http://www.opcfoundation.org/UA/units/un/cefact";

        static List<EUInformation> _UNECEEngineeringUnits;
        public static List<EUInformation> UNECEEngineeringUnits
        {
            get
            {
                if (_UNECEEngineeringUnits == null)
                {
                    // Load UNECE units if not already loaded
                    _UNECEEngineeringUnits = new List<EUInformation>();
                    var fileName = Path.Combine(Path.GetDirectoryName(typeof(VariableModel).Assembly.Location), "NodeSets", "UNECE_to_OPCUA.csv");
                    Stream fileStream;
                    if (File.Exists(fileName))
                    {
                        fileStream = File.OpenRead(fileName);
                    }
                    else
                    {
                        fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CESMII.OpcUa.NodeSetModel.Factory.Opc.NodeSets.UNECE_to_OPCUA.csv");
                    }
                    var parser = new CsvTextFieldParser(fileStream);
                    if (!parser.EndOfData)
                    {
                        var headerFields = parser.ReadFields();
                    }
                    while (!parser.EndOfData)
                    {
                        var parts = parser.ReadFields();
                        if (parts.Length != 4)
                        {
                            // error
                        }
                        var UNECECode = parts[0];
                        var UnitId = parts[1];
                        var DisplayName = parts[2];
                        var Description = parts[3];
                        var newEuInfo = new EUInformation(DisplayName, Description, strUNECEUri)
                        {
                            UnitId = int.Parse(UnitId),
                        };
                        _UNECEEngineeringUnits.Add(newEuInfo);
                    }
                }

                return _UNECEEngineeringUnits;
            }
        }

        static Dictionary<string, List<EUInformation>> _euInformationByDescription;
        static Dictionary<string, List<EUInformation>> EUInformationByDescription
        {
            get
            {
                if (_euInformationByDescription == null)
                {
                    _euInformationByDescription = new Dictionary<string, List<EUInformation>>();
                    foreach (var aEuInformation in UNECEEngineeringUnits)
                    {
                        if (!_euInformationByDescription.ContainsKey(aEuInformation.Description.Text))
                        {
                            _euInformationByDescription.Add(aEuInformation.Description.Text, new List<EUInformation> { aEuInformation });
                        }
                        else
                        {
                            _euInformationByDescription[aEuInformation.DisplayName.Text].Add(aEuInformation);
                        }
                    }
                }
                return _euInformationByDescription;
            }
        }

        static Dictionary<string, List<EUInformation>> _euInformationByDisplayName;
        static Dictionary<string, List<EUInformation>> EUInformationByDisplayName
        {
            get
            {
                if (_euInformationByDisplayName == null)
                {
                    _euInformationByDisplayName = new Dictionary<string, List<EUInformation>>();
                    foreach (var aEuInformation in UNECEEngineeringUnits)
                    {
                        if (!_euInformationByDisplayName.ContainsKey(aEuInformation.DisplayName.Text))
                        {
                            _euInformationByDisplayName.Add(aEuInformation.DisplayName.Text, new List<EUInformation> { aEuInformation });
                        }
                        else
                        {
                            _euInformationByDisplayName[aEuInformation.DisplayName.Text].Add(aEuInformation);
                        }
                    }
                }
                return _euInformationByDisplayName;
            }
        }

        public static EUInformation GetEUInformation(VariableModel.EngineeringUnitInfo engineeringUnitDescription)
        {
            if (engineeringUnitDescription == null) return null;

            List<EUInformation> euInfoList;
            if (!string.IsNullOrEmpty(engineeringUnitDescription.DisplayName?.Text)
                && engineeringUnitDescription.UnitId == null
                && engineeringUnitDescription.Description == null
                && (string.IsNullOrEmpty(engineeringUnitDescription.NamespaceUri) || engineeringUnitDescription.NamespaceUri == strUNECEUri))
            {
                // If we only have a displayname, assume it's a UNECE unit
                // Try to lookup engineering unit by description
                if (EUInformationByDescription.TryGetValue(engineeringUnitDescription.DisplayName.Text, out euInfoList))
                {
                    return euInfoList.FirstOrDefault();
                }
                // Try to lookup engineering unit by display name
                else if (EUInformationByDisplayName.TryGetValue(engineeringUnitDescription.DisplayName.Text, out euInfoList))
                {
                    return euInfoList.FirstOrDefault();
                }
                else
                {
                    // No unit found: just use the displayname
                    return new EUInformation(engineeringUnitDescription.DisplayName.Text, engineeringUnitDescription.DisplayName.Text, null);
                }
            }
            else
            {
                // Custom EUInfo: use what was specified without further validation
                EUInformation euInfo = new EUInformation(engineeringUnitDescription.DisplayName?.Text, engineeringUnitDescription.Description?.Text, engineeringUnitDescription.NamespaceUri);
                if (engineeringUnitDescription.UnitId != null)
                {
                    euInfo.UnitId = engineeringUnitDescription.UnitId.Value;
                }
                return euInfo;
            }
        }

        public static void UpdateAllMethodArgumentVariables(this NodeSetModel nodeSetModel, IOpcUaContext opcContext)
        {
            foreach(var nodeModel in nodeSetModel.AllNodesByNodeId.SelectMany(kv => kv.Value.Methods))
            {
                UpdateMethodArgumentVariables(nodeModel as MethodModel, opcContext);
            }
        }

        public static void UpdateMethodArgumentVariables(MethodModel methodModel, IOpcUaContext opcContext)
        {
            UpdateMethodArgumentVariable(methodModel, BrowseNames.InputArguments, methodModel.InputArguments, opcContext);
            UpdateMethodArgumentVariable(methodModel, BrowseNames.OutputArguments, methodModel.OutputArguments, opcContext);
        }
        private static void UpdateMethodArgumentVariable(MethodModel methodModel, string browseName, List<VariableModel> modelArguments, IOpcUaContext opcContext)
        {
            var argumentProperty = GetArgumentProperty(methodModel, browseName, modelArguments, opcContext);
            if (argumentProperty != null)
            {
                var existingArgumentProperty = methodModel.Properties.FirstOrDefault(p => p.BrowseName == browseName);
                if (existingArgumentProperty == null)
                {
                    methodModel.Properties.Add(argumentProperty);
                }
                else
                {
                    // Update arguments in existing property
                    if (existingArgumentProperty.Value != argumentProperty.Value)
                    {
                        opcContext.Logger.LogInformation($"Updated {browseName} for method {methodModel}");
                        opcContext.Logger.LogTrace($"Updated {browseName} for method {methodModel}. Previous arguments: {existingArgumentProperty.Value}. New arguments: {argumentProperty.Value}");
                        existingArgumentProperty.Value = argumentProperty.Value;
                    }
                }
            }

        }
        internal static PropertyModel GetArgumentProperty(MethodModel methodModel, string browseName, List<VariableModel> modelArguments, IOpcUaContext opcContext)
        { 
            List<Argument> arguments = new List<Argument>();
            if (modelArguments?.Any() == true)
            {
                foreach (var modelArgument in modelArguments)
                {
                    UInt32Collection arrayDimensions = null;
                    if (modelArgument.ArrayDimensions != null)
                    {
                        arrayDimensions = JsonConvert.DeserializeObject<UInt32[]>($"[{modelArgument.ArrayDimensions}]");
                    }

                    var argument = new Argument
                    {
                        Name = modelArgument.BrowseName,
                        ArrayDimensions = arrayDimensions,
                        // TODO parse into array ArrayDimensions = inputArg.ArrayDimensions,
                        ValueRank = modelArgument.ValueRank ?? -1,
                        DataType = ExpandedNodeId.Parse(modelArgument.DataType.NodeId, opcContext.NamespaceUris),
                        Description = new ua.LocalizedText(modelArgument.Description?.FirstOrDefault()?.Text),
                    };
                    if (modelArgument.Value != null || modelArgument.Description.Count > 1 || modelArgument.ModellingRule == "Optional")
                    {
                        // TODO Create or update argumentDescription
                    }
                    arguments.Add(argument);
                }
            }
            if (arguments.Any())
            {
                var argumentDataType = opcContext.GetModelForNode<DataTypeModel>($"nsu={Namespaces.OpcUa};{DataTypeIds.Argument}");
                var argumentPropertyJson = opcContext.JsonEncodeVariant(                    
                    new Variant(arguments.Select(a => new ExtensionObject(a)).ToArray()), 
                    argumentDataType);
                var argumentProperty = new PropertyModel
                {
                    NodeSet = modelArguments[0].NodeSet,
                    NodeId = modelArguments[0].NodeId,
                    CustomState = modelArguments[0].CustomState,
                    BrowseName = opcContext.GetModelBrowseName(browseName),
                    DisplayName = NodeModel.LocalizedText.ListFromText(browseName),
                    Description = new(),
                    Parent = methodModel,
                    DataType = argumentDataType,
                    TypeDefinition = opcContext.GetModelForNode<VariableTypeModel>($"nsu={Namespaces.OpcUa};{VariableTypeIds.PropertyType}"),
                    ValueRank = 1,
                    ArrayDimensions = $"{arguments.Count}",
                    Value = argumentPropertyJson.Json,
                    ModellingRule = "Mandatory",
                };
                return argumentProperty;
            }
            return null;
        }

        /// <summary>
        /// Updates or creates the object of type NamespaceMetaDataType as described in https://reference.opcfoundation.org/Core/Part5/v105/docs/6.3.13
        /// </summary>
        /// <param name="_this"></param>
        public static bool UpdateNamespaceMetaData(this NodeSetModel _this, IOpcUaContext opcContext, bool createIfNotExist = true)
        {
            bool addedMetadata = false;
            var metaDataTypeNodeId = opcContext.GetModelNodeId(ObjectTypeIds.NamespaceMetadataType);
            var serverNamespacesNodeId = opcContext.GetModelNodeId(ObjectIds.Server_Namespaces);
            var metadataObjects = _this.Objects.Where(o => o.TypeDefinition.HasBaseType(metaDataTypeNodeId) && o.Parent.NodeId == serverNamespacesNodeId).ToList();
            var metadataObject = metadataObjects.FirstOrDefault();
            if (metadataObject == null)
            {
                if (!createIfNotExist)
                {
                    return false;
                }
                var parent = opcContext.GetModelForNode<NodeModel>($"nsu={Namespaces.OpcUa};{ObjectIds.Server}");
                metadataObject = new ObjectModel
                {
                    NodeSet = _this,
                    NodeId = GetNewNodeId(_this.ModelUri),
                    DisplayName = new ua.LocalizedText(_this.ModelUri).ToModel(),
                    BrowseName = opcContext.GetModelBrowseName(BrowseNames.NamespaceMetadataType), // $"{Namespaces.OpcUa};{nameof(ObjectTypeIds.NamespaceMetadataType)}",
                    Parent = parent,
                    OtherReferencingNodes = new List<NodeModel.NodeAndReference>
                    {
                        new NodeModel.NodeAndReference
                        {
                             ReferenceType = opcContext.GetModelForNode<NodeModel>($"nsu={Namespaces.OpcUa};{ReferenceTypeIds.HasComponent}"),
                             Node = parent,
                        }
                    }
                };
                _this.Objects.Add(metadataObject);
                addedMetadata = true;
            }
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.NamespaceUri, _this.ModelUri, true);
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.NamespacePublicationDate, _this.PublicationDate, true);
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.NamespaceVersion, _this.Version, true);

            // Only create if not already authored
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.IsNamespaceSubset, "false", false);
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.StaticNodeIdTypes, null, false);
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.StaticNumericNodeIdRange, null, false);
            addedMetadata |= CreateOrReplaceMetaDataProperty(_this, opcContext, metadataObject, BrowseNames.StaticStringNodeIdPattern, null, false);
            return addedMetadata;
        }

        private static bool CreateOrReplaceMetaDataProperty(NodeSetModel _this, IOpcUaContext context, ObjectModel metadataObject, QualifiedName browseName, object value, bool replaceIfExists)
        {
            string modelBrowseName = context.GetModelBrowseName(browseName);
            var previousProp = metadataObject.Properties.FirstOrDefault(p => p.BrowseName == modelBrowseName);
            if (replaceIfExists || previousProp == null)
            {
                string encodedValue;
                if (value is DateTime)
                {
                    encodedValue = $"{{\"Value\":{{\"Type\":13,\"Body\":\"{value:O}\"}}}}";
                }
                else
                {
                    encodedValue = $"{{\"Value\":{{\"Type\":12,\"Body\":\"{value}\"}}}}";
                }
                if (previousProp != null)
                {
                    previousProp.Value = encodedValue;
                }
                else
                {
                    metadataObject.Properties.Add(new PropertyModel
                    {
                        NodeSet = _this,
                        NodeId = GetNewNodeId(_this.ModelUri),
                        BrowseName = modelBrowseName,
                        DisplayName = new ua.LocalizedText(browseName.Name).ToModel(),
                        Value = encodedValue,
                    });
                }
                return true;
            }
            return false;
        }

        public static List<string> UpdateEncodings(this NodeSetModel _this, IOpcUaContext context)
        {
            var missingEncodings = new List<string>();
            foreach (var dataType in _this.DataTypes)
            {
                if (dataType.StructureFields?.Any() == true)
                {
                    // Ensure there's an encoding for the data type
                    var hasEncodingNodeId = context.GetModelNodeId(ReferenceTypeIds.HasEncoding);
                    var encodingReferences = dataType.OtherReferencedNodes.Where(nr => (nr.ReferenceType as ReferenceTypeModel).HasBaseType(hasEncodingNodeId)).ToList();

                    foreach (var encodingBrowseName in new[] { BrowseNames.DefaultXml, BrowseNames.DefaultJson, BrowseNames.DefaultBinary })
                    {
                        var encodingModelBrowseName = context.GetModelBrowseName(encodingBrowseName);
                        if (!encodingReferences.Any(nr => nr.Node.BrowseName == encodingModelBrowseName))
                        {
                            var encodingId = NodeModelOpcExtensions.GetNewNodeId(dataType.Namespace);
                            var encoding = new ObjectModel
                            {
                                NodeSet = dataType.NodeSet,
                                NodeId = encodingId,
                                BrowseName =  encodingModelBrowseName,
                                DisplayName = new ua.LocalizedText(encodingBrowseName).ToModel(),
                                TypeDefinition = context.GetModelForNode<ObjectTypeModel>($"nsu={Namespaces.OpcUa};{ObjectTypeIds.DataTypeEncodingType}"),
                                Parent = dataType,
                            };
                            // According to https://reference.opcfoundation.org/Core/Part6/v105/docs/F.4 only one direction of the reference is required: using inverse reference on the encoding only to keep the data type XML cleaner
                            encoding.OtherReferencingNodes.Add(new NodeModel.NodeAndReference
                            {
                                ReferenceType = context.GetModelForNode<ReferenceTypeModel>($"nsu={Namespaces.OpcUa};{ReferenceTypeIds.HasEncoding}"),
                                Node = dataType,
                            });
                           _this.Objects.Add(encoding);
                            missingEncodings.Add($"{dataType}: {encoding}");
                        }
                    }
                }
            }
            return missingEncodings;
        }

        public static string GetNewNodeId(string nameSpace)
        {
            return new ExpandedNodeId(Guid.NewGuid(), nameSpace).ToString();
        }
        public static string ToModel(this QualifiedName qName, NamespaceTable namespaceUris)
        {
            return $"{namespaceUris.GetString(qName.NamespaceIndex)};{qName.Name}";
        }

        public static string GetNodeClass(this NodeModel nodeModel)
        {
            var type = nodeModel.GetType().Name;
            var nodeClass = type.Substring(0, type.Length - "Model".Length);
            return nodeClass;
        }

    }

}