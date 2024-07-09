using Opc.Ua;
using ua = Opc.Ua;

using System;
using System.Collections.Generic;
using System.Linq;

using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Opc.Ua.Export;
using System.Xml;

namespace CESMII.OpcUa.NodeSetModel.Factory.Opc
{

    public class NodeModelFactoryOpc : NodeModelFactoryOpc<NodeModel>
    {
        public static Task<List<NodeSetModel>> LoadNodeSetAsync(IOpcUaContext opcContext, UANodeSet nodeSet, Object customState, Dictionary<string, string> Aliases, bool doNotReimport = false, List<NodeState> importedNodes = null)
        {
            if (!nodeSet.Models.Any())
            {
                var ex = new Exception($"Invalid nodeset: no models specified");
                opcContext.Logger.LogError(ex.Message);
                throw ex;
            }

            // Find all models that are used by another nodeset
            var requiredModels = nodeSet.Models.Where(m => m.RequiredModel != null).SelectMany(m => m.RequiredModel).Distinct().ToList();
            var missingModels = requiredModels.Where(rm => opcContext.GetOrAddNodesetModel(rm) == null).ToList();
            if (missingModels.Any())
            {
                throw new Exception($"Missing dependent node sets: {string.Join(", ", missingModels)}");
            }

            var loadedModels = new List<NodeSetModel>();

            NodeModelUtils.FixupNodesetVersionFromMetadata(nodeSet, opcContext.Logger);
            foreach (var model in nodeSet.Models)
            {
                var nodesetModel = opcContext.GetOrAddNodesetModel(model);
                if (nodesetModel == null)
                {
                    throw new NodeSetResolverException($"Unable to create node set: {model.ModelUri}");
                }
                nodesetModel.CustomState = customState;
                if (model.RequiredModel != null)
                {
                    foreach (var requiredModel in model.RequiredModel)
                    {
                        var requiredModelInfo = nodesetModel.RequiredModels.FirstOrDefault(rm => rm.ModelUri == requiredModel.ModelUri);
                        if (requiredModelInfo == null)
                        {
                            throw new Exception("Required model not found");
                        }
                        if (requiredModelInfo != null && requiredModelInfo.AvailableModel == null)
                        {
                            var availableModel = opcContext.GetOrAddNodesetModel(requiredModel);
                            if (availableModel != null)
                            {
                                requiredModelInfo.AvailableModel = availableModel;
                            }
                        }
                    }
                }
                if (nodeSet.Aliases?.Length > 0 && Aliases != null)
                {
                    foreach (var alias in nodeSet.Aliases)
                    {
                        Aliases[alias.Value] = alias.Alias;
                    }
                }
                loadedModels.Add(nodesetModel);
            }
            if (nodeSet.Items == null)
            {
                nodeSet.Items = new UANode[0];
            }

            var newImportedNodes = opcContext.ImportUANodeSet(nodeSet);

            // TODO Read nodeset poperties like author etc. and expose them in Profile editor

            foreach (var node in newImportedNodes)
            {
                var nodeModel = NodeModelFactoryOpc.Create(opcContext, node, customState, out var bAdded);
                if (nodeModel != null && !bAdded)
                {
                    var nodesetModel = nodeModel.NodeSet;

                    if (!nodesetModel.AllNodesByNodeId.ContainsKey(nodeModel.NodeId))
                    {
                        nodesetModel.UnknownNodes.Add(nodeModel);
                    }
                }
            }
            
            // Ensure references that are implicitly used by the importer get resolved into the OPC model
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(ReferenceTypes.HasSubtype), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(ReferenceTypes.HasModellingRule), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(Objects.ModellingRule_Mandatory), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(Objects.ModellingRule_Optional), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(Objects.ModellingRule_ExposesItsArray), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(Objects.ModellingRule_MandatoryPlaceholder), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(Objects.ModellingRule_OptionalPlaceholder), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(ReferenceTypes.HasTypeDefinition), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(ReferenceTypes.GeneratesEvent), null, out _);
            ReferenceTypeModelFactoryOpc.Create(opcContext, opcContext.GetNode(ReferenceTypes.Organizes), null, out _);

            if (importedNodes != null)
            {
                importedNodes.AddRange(newImportedNodes);
            }
            return Task.FromResult(loadedModels);
        }
    }
    public class NodeModelFactoryOpc<TNodeModel> where TNodeModel : NodeModel, new()
    {
        protected TNodeModel _model;
        protected ILogger Logger;

        protected virtual void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            Logger.LogTrace($"Creating node model for {opcNode}");
            // TODO capture multiple locales from a nodeset: UA library seems to offer only one locale
            _model.DisplayName = opcNode.DisplayName.ToModel();

            var browseNameNamespace = opcContext.NamespaceUris.GetString(opcNode.BrowseName.NamespaceIndex);
            _model.BrowseName = opcContext.GetModelBrowseName(opcNode.BrowseName);
            _model.SymbolicName = opcNode.SymbolicName;
            _model.Description = opcNode.Description.ToModel();
            if (opcNode.Categories != null)
            {
                if (_model.Categories == null)
                {
                    _model.Categories = new List<string>();
                }
                _model.Categories.AddRange(opcNode.Categories);
            }
            _model.Documentation = opcNode.NodeSetDocumentation;
            _model.ReleaseStatus = opcNode.ReleaseStatus.ToString();

            if (recursionDepth <= 0)
            {
                _model.ReferencesNotResolved = true;
                return;
            }

            recursionDepth--;
            _model.ReferencesNotResolved = false;

            var references = opcContext.GetHierarchyReferences(opcNode);

            foreach (var reference in references)
            {
                var referenceType = opcContext.GetNode(reference.ReferenceTypeId) as ReferenceTypeState;
                if (referenceType == null)
                {
                    throw new Exception($"Reference Type {reference.ReferenceTypeId} not found for reference from {opcNode} to {reference.TargetId} . Missing required model / node set?");
                }
                var referenceTypes = GetBaseTypes(opcContext, referenceType);

                var referencedNode = opcContext.GetNode(reference.TargetId);
                if (referencedNode == null)
                {
                    throw new Exception($"Referenced node {reference.TargetId} not found for {opcNode}");
                }

                if (reference.IsInverse)
                {
                    // TODO UANodeSet.Import should already handle inverse references: investigate why these are not processed
                    // Workaround for now:
                    AddChildToNodeModel(
                        () => NodeModelFactoryOpc<TNodeModel>.Create(opcContext, referencedNode, this._model.CustomState, out _, recursionDepth),
                        opcContext, referenceType, referenceTypes, opcNode, recursionDepth);
                }
                else
                {
                    AddChildToNodeModel(() => this._model, opcContext, referenceType, referenceTypes, referencedNode, recursionDepth);
                }
            }
            Logger.LogTrace($"Created node model {this._model} for {opcNode}");
        }

        private static void AddChildToNodeModel(Func<NodeModel> parentFactory, IOpcUaContext opcContext, ReferenceTypeState referenceType, List<BaseTypeState> referenceTypes, NodeState referencedNode, int recursionDepth)
        {
            var organizesNodeId = opcContext.GetModelNodeId(ReferenceTypeIds.Organizes);
            if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasComponent))
            {
                if (referencedNode is BaseObjectState objectState)
                {
                    // NodeModel.Objects
                    var parent = parentFactory();
                    var uaChildObject = Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, objectState, parent?.CustomState, recursionDepth);
                    if (uaChildObject != null)
                    {
                        var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                        if (parent?.Namespace != uaChildObject.Namespace)
                        {
                            // Add the reverse reference to the referencing node (parent)
                            var referencingNodeAndReference = new NodeModel.NodeAndReference { Node = parent, ReferenceType = referenceTypeModel };
                            AddChildIfNotExists(uaChildObject, uaChildObject.OtherReferencingNodes, referencingNodeAndReference, opcContext.Logger, organizesNodeId, false);
                        }
                        AddChildIfNotExists(parent, parent?.Objects, uaChildObject, opcContext.Logger, organizesNodeId);
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.HasComponent)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaChildObject, ReferenceType = referenceTypeModel };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, false);
                        }
                    }
                }
                else if (referencedNode is BaseObjectTypeState objectTypeState)
                {
                    opcContext.Logger.LogWarning($"Ignoring component {referencedNode} with unexpected node type {referencedNode.GetType()}");
                }
                else if (referencedNode is BaseDataVariableState variableState)
                {
                    // NodeModel.DataVariables
                    if (ProcessEUInfoAndRanges(opcContext, referencedNode, parentFactory))
                    {
                        // EU Information was captured in the parent model
                        return;
                    }
                    var parent = parentFactory();
                    var variable = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, variableState, parent?.CustomState, recursionDepth);
                    AddChildIfNotExists(parent, parent?.DataVariables, variable, opcContext.Logger, organizesNodeId);
                    var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                    if (referenceTypes[0].NodeId != ReferenceTypeIds.HasComponent)
                    {
                        // Preserve the more specific reference type as well
                        var nodeAndReference = new NodeModel.NodeAndReference { Node = variable, ReferenceType = referenceTypeModel };
                        AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, false);
                    }
                }
                else if (referencedNode is MethodState methodState)
                {
                    // NodeModel.Methods
                    var parent = parentFactory();
                    var method = Create<MethodModelFactoryOpc, MethodModel>(opcContext, methodState, parent?.CustomState, recursionDepth);
                    AddChildIfNotExists(parent, parent?.Methods, method, opcContext.Logger, organizesNodeId);
                }
                else if(referencedNode is PropertyState propertyState)
                {
                    // Not allowed per spec, but tolerate (treat as Property)
                    var parent = parentFactory();
                    var property = Create<PropertyModelFactoryOpc, PropertyModel>(opcContext, propertyState, parent?.CustomState, recursionDepth);
                    AddChildIfNotExists(parent, parent?.Properties, property, opcContext.Logger, organizesNodeId);
                    var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                    if (referenceTypes[0].NodeId != ReferenceTypeIds.HasComponent)
                    {
                        // Preserve the more specific reference type as well
                        var nodeAndReference = new NodeModel.NodeAndReference { Node = property, ReferenceType = referenceTypeModel };
                        AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, false);
                    }
                }
                else
                {
                    var parent = parentFactory();
                    if (referencedNode != null)
                    {
                        throw new Exception($"Property {referencedNode} has unexpected type {referencedNode.GetType()} in {parent}");
                }
                    throw new Exception($"Property {referencedNode} not found in {parent}");
            }
            }
            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasProperty))
            {
                // NodeModel.Properties
                if (ProcessEUInfoAndRanges(opcContext, referencedNode, parentFactory))
                {
                    // EU Information was captured in the parent model
                    return;
                }
                // OptionSetValues are not commonly used and if they are they don't differ from the enum definitiones except for reserved bits: just preserve as regular properties/values for now so we can round trip without designer support
                //else if (referencedNode.BrowseName?.Name == BrowseNames.OptionSetValues)
                //{
                //    var parent = parentFactory();
                //    if (parent is DataTypeModel dataType && dataType != null)
                //    {
                //        var optionSetValues = ((referencedNode as BaseVariableState)?.Value as LocalizedText[]);
                //        if (optionSetValues != null)
                //        {
                //            dataType.SetOptionSetValues(optionSetValues.ToModel());
                //            return;
                //        }
                //        else
                //        {
                //            opcContext.Logger.LogInformation($"No or invalid OptionSetValues in {parent} for {referencedNode}");
                //        }
                //    }
                //    else
                //    {
                //        opcContext.Logger.LogInformation($"Unexpected parent {parent} of type {parent.GetType()} for OptionSetValues property {referencedNode}");
                //    }
                //}
                if (referencedNode is PropertyState propertyState)
                {
                    var parent = parentFactory();
                    var property = Create<PropertyModelFactoryOpc, PropertyModel>(opcContext, propertyState, parent?.CustomState, recursionDepth);
                    AddChildIfNotExists(parent, parent?.Properties, property, opcContext.Logger, organizesNodeId);
                    var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                    if (referenceTypes[0].NodeId != ReferenceTypeIds.HasProperty)
                    {
                        // Preserve the more specific reference type as well
                        var nodeAndReference = new NodeModel.NodeAndReference { Node = property, ReferenceType = referenceTypeModel };
                        AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, false);
                    }
                }
                else if (referencedNode is BaseDataVariableState variableState)
                {
                    // Surprisingly, properties can also be of type DataVariable
                    var parent = parentFactory();
                    var variable = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, variableState, parent?.CustomState, recursionDepth);
                    AddChildIfNotExists(parent, parent?.Properties, variable, opcContext.Logger, organizesNodeId);
                    var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                    if (referenceTypes[0].NodeId != ReferenceTypeIds.HasProperty)
                    {
                        // Preserve the more specific reference type as well
                        var nodeAndReference = new NodeModel.NodeAndReference { Node = variable, ReferenceType = referenceTypeModel };
                        AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, false);
                    }
                }
                else
                {
                    var parent = parentFactory();
                    if (referencedNode != null)
                    {
                        throw new Exception($"Property {referencedNode} has unexpected type {referencedNode.GetType()} in {parent}");
                }
                    throw new Exception($"Property {referencedNode} not found in {parent}");

                }
            }
            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasInterface))
            {
                // NodeModel.Interfaces
                if (referencedNode is BaseObjectTypeState interfaceTypeState)
                {
                    var parent = parentFactory();
                    var uaInterface = Create<InterfaceModelFactoryOpc, InterfaceModel>(opcContext, interfaceTypeState, parent?.CustomState, recursionDepth);
                    if (uaInterface != null)
                    {
                        AddChildIfNotExists(parent, parent?.Interfaces, uaInterface, opcContext.Logger, organizesNodeId);
                        var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.HasInterface)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaInterface, ReferenceType = referenceTypeModel };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId);
                        }
                    }
                }
                else
                {
                    var parent = parentFactory();
                    if (referencedNode != null)
                    {
                        throw new Exception($"Interface {referencedNode} has unexpected type {referencedNode.GetType()} in {parent}");
                    }
                    throw new Exception($"Interface {referencedNode} not found in {parent}");
                }
            }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.Organizes))
            //{
            //    if (referencedNode is BaseObjectState)
            //    {
            //        var parent = parentFactory();
            //        var organizedNode = Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, referencedNode, parent.CustomState);
            //        AddChildIfNotExists(parent, parent?.Objects, organizedNode, opcContext.Logger);
            //    }
            //    else
            //    {

            //    }
            //}
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.FromState))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.ToState))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasEffect))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasCause))
            //{ }
            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.GeneratesEvent))
            {
                // NodeModel.Events
                if (referencedNode is BaseObjectTypeState eventTypeState)
                {
                    var parent = parentFactory();
                    var uaEvent = Create<ObjectTypeModelFactoryOpc, ObjectTypeModel>(opcContext, eventTypeState, parent?.CustomState, recursionDepth);
                    if (uaEvent != null)
                    {
                        AddChildIfNotExists(parent, parent?.Events, uaEvent, opcContext.Logger, organizesNodeId);
                        var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.GeneratesEvent)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaEvent, ReferenceType = referenceTypeModel };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId);
                        }
                    }
                }
                else
                {
                    var parent = parentFactory();
                    throw new Exception($"Unexpected event type {referencedNode} in {parent}");
                }
            }
            else
            {
                // NodeModel.OtherReferencedNodes
                var parent = parentFactory();
                var referencedModel = Create(opcContext, referencedNode, parent?.CustomState, out _, recursionDepth);
                if (referencedModel != null)
                {
                    var referenceTypeModel = ReferenceTypeModelFactoryOpc.Create(opcContext, referenceType, null, out _, recursionDepth) as ReferenceTypeModel;
                    var nodeAndReference = new NodeModel.NodeAndReference
                    {
                        Node = referencedModel,
                        ReferenceType = referenceTypeModel
                    };
                    AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, organizesNodeId, true);

                    // Add the reverse reference to the referencing node (parent)
                    var referencingNodeAndReference = new NodeModel.NodeAndReference { Node = parent, ReferenceType = referenceTypeModel };
                    AddChildIfNotExists(referencedModel, referencedModel.OtherReferencingNodes, referencingNodeAndReference, opcContext.Logger, organizesNodeId, false);
                }
                else
                {
                    new Exception($"Failed to resolve reference {referenceTypes.FirstOrDefault()} from {parent} to {referencedNode}.");
                }
                // Potential candidates for first class representation in the model:
                // {ns=1;i=6030} - ConnectsTo / Hierarchical
                // {ns=2;i=18179} - Requires / Hierarchical
                // {ns=2;i=18178} - Moves / Hierarchical
                // {ns=2;i=18183} - HasSlave / Hierachical
                // {ns=2;i=18180} - IsDrivenBy / Hierarchical
                // {ns=2;i=18182} - HasSafetyStates - Hierarchical
                // {ns=2;i=4002}  - Controls / Hierarchical
            }

        }
        static void AddChildIfNotExists<TColl>(NodeModel parent, IList<TColl> collection, TColl uaChildObject, ILogger logger, string organizesNodeId, bool setParent = true)
        {
            if (uaChildObject == null)
            {
                return;
            }
            if (setParent
                && (uaChildObject is InstanceModelBase uaInstance
                    || (uaChildObject is NodeModel.NodeAndReference nr
                       && (nr.ReferenceType as ReferenceTypeModel)?.HasBaseType(organizesNodeId) == true
                       && (uaInstance = (nr.Node as InstanceModelBase)) != null)
                       ))
            {
                uaInstance.Parent = parent;
                if (uaInstance.Parent != parent)
                {
                    logger.LogInformation($"{uaInstance} has more than one parent. Ignored parent: {parent}, using {uaInstance.Parent}");
                }
            }
            if (collection?.Contains(uaChildObject) == false)
            {
                collection.Add(uaChildObject);
            }
        }

        static bool ProcessEUInfoAndRangesWithoutParent(IOpcUaContext opcContext, NodeState potentialEUNode, object customState)
        {
            if (potentialEUNode.BrowseName?.Name == BrowseNames.EngineeringUnits || (potentialEUNode as BaseVariableState)?.DataType == DataTypeIds.EUInformation
                || potentialEUNode.BrowseName?.Name == BrowseNames.EURange || potentialEUNode.BrowseName?.Name == BrowseNames.InstrumentRange)
            {
                foreach (var referenceToNode in opcContext.GetHierarchyReferences(potentialEUNode).Where(r => r.IsInverse))
                {
                    var referencingNodeState = opcContext.GetNode(referenceToNode.TargetId);
                    var referencingNode = Create(opcContext, referencingNodeState, customState, out _);
                    if (ProcessEUInfoAndRanges(opcContext, potentialEUNode, () => referencingNode))
                    {
                        // captured in the referencing node
                        return true;
                    }
                }
            }
            return false;
        }
        static bool ProcessEUInfoAndRanges(IOpcUaContext opcContext, NodeState referencedNode, Func<NodeModel> parentFactory)
        {
            if (referencedNode.BrowseName?.Name == BrowseNames.EngineeringUnits || (referencedNode as BaseVariableState).DataType == DataTypeIds.EUInformation)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    parentVariable.EngUnitNodeId = opcContext.GetModelNodeId(referencedNode.NodeId);

                    var modellingRuleId = (referencedNode as BaseInstanceState)?.ModellingRuleId;
                    if (modellingRuleId != null)
                    {
                        var modellingRule = opcContext.GetNode(modellingRuleId);
                        if (modellingRule == null)
                        {
                            throw new Exception($"Unable to resolve modelling rule {modellingRuleId}: dependency on UA nodeset not declared?");
                        }
                        parentVariable.EngUnitModellingRule = modellingRule.DisplayName.Text;
                    }
                    if (referencedNode is BaseVariableState euInfoVariable)
                    {
                        parentVariable.EngUnitAccessLevel = euInfoVariable.AccessLevelEx != 1 ? euInfoVariable.AccessLevelEx : null;
                        // deprecated: parentVariable.EngUnitUserAccessLevel = euInfoVariable.UserAccessLevel != 1 ? euInfoVariable.UserAccessLevel : null;

                        var euInfoExtension = euInfoVariable.Value as ExtensionObject;
                        var euInfo = euInfoExtension?.Body as EUInformation;
                        if (euInfo != null)
                        {
                            parentVariable.SetEngineeringUnits(euInfo);
                        }
                        else
                        {
                            if (euInfoVariable.Value != null)
                            {
                                if (euInfoExtension != null)
                                {
                                    if (euInfoExtension.TypeId != ObjectIds.EUInformation_Encoding_DefaultXml)
                                    {
                                        throw new Exception($"Unable to parse Engineering units for {parentVariable}: Invalid encoding type id {euInfoExtension.TypeId}. Expected {ObjectIds.EUInformation_Encoding_DefaultXml}.");
                                    }
                                    if (euInfoExtension.Body is XmlElement xmlValue)
                                    {
                                        throw new Exception($"Unable to parse Engineering units for {parentVariable}: TypeId: {euInfoExtension.TypeId}.XML: {xmlValue.OuterXml}.");
                                    }
                                    throw new Exception($"Unable to parse Engineering units for {parentVariable}: TypeId: {euInfoExtension.TypeId}. Value: {(referencedNode as BaseVariableState).Value}");
                                }
                                throw new Exception($"Unable to parse Engineering units for {parentVariable}: {(referencedNode as BaseVariableState).Value}");
                            }
                            // Nodesets commonly indicate that EUs are required on instances by specifying an empty EU in the class
                        }
                    }
                    return true;
                }
            }
            else if (referencedNode.BrowseName?.Name == BrowseNames.EURange)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    var info = GetRangeInfo(parentVariable, referencedNode, opcContext);
                    parentVariable.EURangeNodeId = info.RangeNodeId;
                    parentVariable.EURangeModellingRule = info.ModellingRuleId;
                    parentVariable.EURangeAccessLevel = info.rangeAccessLevel;
                    if (info.range != null)
                    {
                        parentVariable.SetRange(info.range);
                    }
                    return true;
                }
            }
            else if (referencedNode.BrowseName?.Name == BrowseNames.InstrumentRange)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    var info = GetRangeInfo(parentVariable, referencedNode, opcContext);
                    parentVariable.InstrumentRangeNodeId = info.RangeNodeId;
                    parentVariable.InstrumentRangeModellingRule = info.ModellingRuleId;
                    parentVariable.InstrumentRangeAccessLevel = info.rangeAccessLevel;
                    if (info.range != null)
                    {
                        parentVariable.SetInstrumentRange(info.range);
                    }
                    return true;
                }
            }
            return false;
        }

        static (ua.Range range, string RangeNodeId, string ModellingRuleId, uint? rangeAccessLevel)
            GetRangeInfo(NodeModel parentVariable, NodeState referencedNode, IOpcUaContext opcContext)
        {
            string rangeNodeId = opcContext.GetModelNodeId(referencedNode.NodeId);
            string rangeModellingRule = null;
            uint? rangeAccessLevel = null;
            ua.Range range = null;
            var modellingRuleId = (referencedNode as BaseInstanceState)?.ModellingRuleId;
            if (modellingRuleId != null)
            {
                var modellingRuleNode = opcContext.GetNode(modellingRuleId);
                if (modellingRuleNode == null)
                {
                    throw new Exception($"Unable to resolve modelling rule {modellingRuleId}: dependency on UA nodeset not declared?");
                }
                rangeModellingRule = modellingRuleNode.DisplayName.Text;
            }
            if (referencedNode is BaseVariableState euRangeVariable)
            {
                rangeAccessLevel = euRangeVariable.AccessLevelEx != 1 ? euRangeVariable.AccessLevelEx : null;
                // deprecated: parentVariable.EURangeUserAccessLevel = euRangeVariable.UserAccessLevel != 1 ? euRangeVariable.UserAccessLevel : null;

                var euRangeExtension = euRangeVariable.Value as ExtensionObject;
                range = euRangeExtension?.Body as ua.Range;
                if (range == null)
                {
                    if (euRangeVariable.Value != null)
                    {
                        if (euRangeExtension != null)
                        {
                            if (euRangeExtension.TypeId != ObjectIds.Range_Encoding_DefaultXml)
                            {
                                throw new Exception($"Unable to parse {referencedNode.BrowseName?.Name} for {parentVariable}: Invalid encoding type id {euRangeExtension.TypeId}. Expected {ObjectIds.Range_Encoding_DefaultXml}.");
                            }
                            if (euRangeExtension.Body is XmlElement xmlValue)
                            {
                                throw new Exception($"Unable to parse {referencedNode.BrowseName?.Name} for {parentVariable}: TypeId: {euRangeExtension.TypeId}.XML: {xmlValue.OuterXml}.");
                            }
                            throw new Exception($"Unable to parse {referencedNode.BrowseName?.Name} for {parentVariable}: TypeId: {euRangeExtension.TypeId}. Value: {(referencedNode as BaseVariableState).Value}");
                        }
                        throw new Exception($"Unable to parse {referencedNode.BrowseName?.Name} for {parentVariable}: {(referencedNode as BaseVariableState).Value}");
                    }
                    // Nodesets commonly indicate that EURange are required on instances by specifying an enpty EURange in the class
                }
            }
            return (range, rangeNodeId, rangeModellingRule, rangeAccessLevel);
        }


        public static NodeModel Create(IOpcUaContext opcContext, NodeState node, object customState, out bool added, int recursionDepth = int.MaxValue)
        {
            NodeModel nodeModel;
            added = true;
            if (node is DataTypeState dataType)
            {
                nodeModel = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType, customState, recursionDepth);
            }
            else if (node is BaseVariableTypeState variableType)
            {
                nodeModel = Create<VariableTypeModelFactoryOpc, VariableTypeModel>(opcContext, variableType, customState, recursionDepth);
            }
            else if (node is BaseObjectTypeState objectType)
            {
                if (GetBaseTypes(opcContext, objectType).Any(n => n.NodeId == ObjectTypeIds.BaseInterfaceType))
                {
                    nodeModel = Create<InterfaceModelFactoryOpc, InterfaceModel>(opcContext, objectType, customState, recursionDepth);
                }
                else
                {
                    nodeModel = Create<ObjectTypeModelFactoryOpc, ObjectTypeModel>(opcContext, objectType, customState, recursionDepth);
                }
            }
            else if (node is BaseObjectState uaObject)
            {
                nodeModel = Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, uaObject, customState, recursionDepth);
            }
            else if (node is PropertyState property)
            {
                nodeModel = Create<PropertyModelFactoryOpc, PropertyModel>(opcContext, property, customState, recursionDepth);
            }
            else if (node is BaseDataVariableState dataVariable)
            {
                nodeModel = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, dataVariable, customState, recursionDepth);
            }
            else if (node is MethodState methodState)
            {
                nodeModel = Create<MethodModelFactoryOpc, MethodModel>(opcContext, methodState, customState, recursionDepth);
            }
            else if (node is ReferenceTypeState referenceState)
            {
                nodeModel = Create<ReferenceTypeModelFactoryOpc, ReferenceTypeModel>(opcContext, referenceState, customState, recursionDepth);
            }
            else
            {
                if (!(node is ViewState))
                {
                    nodeModel = Create<NodeModelFactoryOpc<TNodeModel>, TNodeModel>(opcContext, node, customState, recursionDepth);
                }
                else
                {
                    // TODO support Views
                    nodeModel = null;
                }
                added = false;
            }
            return nodeModel;

        }

        public static List<BaseTypeState> GetBaseTypes(IOpcUaContext opcContext, BaseTypeState objectType)
        {
            var baseTypes = new List<BaseTypeState>();
            if (objectType != null)
            {
                baseTypes.Add(objectType);
            }
            var currentObjectType = objectType;
            while (currentObjectType?.SuperTypeId != null)
            {
                var objectSuperType = opcContext.GetNode(currentObjectType.SuperTypeId);
                if (objectSuperType is BaseTypeState)
                {
                    baseTypes.Add(objectSuperType as BaseTypeState);
                }
                else
                {
                    baseTypes.Add(new BaseObjectTypeState { NodeId = objectType.SuperTypeId, Description = "Unknown type: more base types may exist" });
                }
                currentObjectType = objectSuperType as BaseTypeState;
            }
            return baseTypes;
        }


        protected static TNodeModel2 Create<TNodeModelOpc, TNodeModel2>(IOpcUaContext opcContext, NodeState opcNode, object customState, int recursionDepth) where TNodeModelOpc : NodeModelFactoryOpc<TNodeModel2>, new() where TNodeModel2 : NodeModel, new()
        {
            var nodeId = opcContext.GetModelNodeId(opcNode.NodeId);

            // EngineeringUnits are captured in the datavariable to which they belong in order to simplify the model for consuming applications
            // Need to make sure that the nodes with engineering units get properly captured even if they are processed before the containing node
            if (ProcessEUInfoAndRangesWithoutParent(opcContext, opcNode, customState))
            {
                // Node was captured into a parent: don't create separate model for it
                return null;
            }
            string namespaceUri = opcContext.NamespaceUris.GetString(opcNode.NodeId.NamespaceIndex);
            var nodeModel = Create<TNodeModel2>(opcContext, nodeId, new ModelTableEntry { ModelUri = namespaceUri }, customState, out var created);
            var nodeModelOpc = new TNodeModelOpc { _model = nodeModel, Logger = opcContext.Logger };
            if (created || nodeModel.ReferencesNotResolved)
            {
                nodeModelOpc.Initialize(opcContext, opcNode, recursionDepth);
            }
            else
            {
                opcContext.Logger.LogTrace($"Using previously created node model {nodeModel} for {opcNode}");
            }
            return nodeModel;
        }

        public static TNodeModel2 Create<TNodeModel2>(IOpcUaContext opcContext, string nodeId, ModelTableEntry opcModelInfo, object customState, out bool created) where TNodeModel2 : NodeModel, new()
        {
            created = false;
            opcContext.NamespaceUris.GetIndexOrAppend(opcModelInfo.ModelUri); // Ensure the namespace is in the namespace table
            var nodeModelBase = opcContext.GetModelForNode<TNodeModel2>(nodeId);
            var nodeModel = nodeModelBase as TNodeModel2;
            if (nodeModel == null)
            {
                if (nodeModelBase != null)
                {
                    throw new Exception($"Internal error - Type mismatch for node {nodeId}: NodeModel of type {typeof(TNodeModel2)} was previously created with type {nodeModelBase.GetType()}.");
                }
                var nodesetModel = opcContext.GetOrAddNodesetModel(opcModelInfo);

                nodeModel = new TNodeModel2();
                nodeModel.NodeSet = nodesetModel;
                if (nodesetModel.CustomState == null)
                {
                    nodesetModel.CustomState = customState;
                }
                nodeModel.NodeId = nodeId;
                nodeModel.CustomState = customState;
                created = true;

                if (!nodesetModel.AllNodesByNodeId.ContainsKey(nodeModel.NodeId))
                {
                    nodesetModel.AllNodesByNodeId.Add(nodeModel.NodeId, nodeModel);
                    if (nodeModel is InterfaceModel uaInterface)
                    {
                        nodesetModel.Interfaces.Add(uaInterface);
                    }
                    else if (nodeModel is ObjectTypeModel objectType)
                    {
                        nodesetModel.ObjectTypes.Add(objectType);
                    }
                    else if (nodeModel is DataTypeModel dataType)
                    {
                        nodesetModel.DataTypes.Add(dataType);
                    }
                    else if (nodeModel is DataVariableModel dataVariable)
                    {
                        nodesetModel.DataVariables.Add(dataVariable);
                    }
                    else if (nodeModel is VariableTypeModel variableType)
                    {
                        nodesetModel.VariableTypes.Add(variableType);
                    }
                    else if (nodeModel is ObjectModel uaObject)
                    {
                        nodesetModel.Objects.Add(uaObject);
                    }
                    else if (nodeModel is PropertyModel property)
                    {
                        nodesetModel.Properties.Add(property);
                    }
                    else if (nodeModel is MethodModel method)
                    {
                        nodesetModel.Methods.Add(method);    
                    }
                    else if (nodeModel is ReferenceTypeModel referenceType)
                    {
                        nodesetModel.ReferenceTypes.Add(referenceType);
                    }
                    else
                    {
                        throw new Exception($"Unexpected node model type {nodeModel.GetType().FullName} for node {nodeModel}");
                    }
                }
                else
                {
                    // Node already processed
                    opcContext.Logger.LogWarning($"Node {nodeModel} was already in the nodeset model.");
                }
            }
            if (customState != null && nodeModel != null && nodeModel.CustomState == null)
            {
                nodeModel.CustomState = customState;
            }
            return nodeModel;
        }

    }

    public class InstanceModelFactoryOpc<TInstanceModel, TBaseTypeModel, TBaseTypeModelFactoryOpc> : NodeModelFactoryOpc<TInstanceModel>
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : NodeModel, new()
        where TBaseTypeModelFactoryOpc : NodeModelFactoryOpc<TBaseTypeModel>, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            var uaInstance = opcNode as BaseInstanceState;
            var variableTypeDefinition = opcContext.GetNode(uaInstance.TypeDefinitionId);
            if (variableTypeDefinition != null) //is BaseTypeState)
            {
                var typeDefModel = NodeModelFactoryOpc.Create(opcContext, variableTypeDefinition, _model.CustomState, out _, recursionDepth -1); // Create<TBaseTypeModelFactoryOpc, TBaseTypeModel>(opcContext, variableTypeDefinition, null);
                _model.TypeDefinition = typeDefModel as TBaseTypeModel;
                if (_model.TypeDefinition == null)
                {
                    throw new Exception($"Unexpected type definition {variableTypeDefinition} on {uaInstance}");
                }
            }

            if (uaInstance.ModellingRuleId != null)
            {
                var modellingRuleId = uaInstance.ModellingRuleId;
                var modellingRule = opcContext.GetNode(modellingRuleId);
                if (modellingRule == null)
                {
                    throw new Exception($"Unable to resolve modelling rule {modellingRuleId}: dependency on UA nodeset not declared?");
                }
                _model.ModellingRule = modellingRule.DisplayName.Text;
            }
            if (uaInstance.Parent != null)
            {
                var instanceParent = NodeModelFactoryOpc.Create(opcContext, uaInstance.Parent, null, out _, recursionDepth - 1);
                _model.Parent = instanceParent;
                if (_model.Parent != instanceParent)
                {
                    opcContext.Logger.LogWarning($"{_model} has more than one parent. Ignored parent: {instanceParent}, using {_model.Parent}.");
                }
            }
        }

    }

    public class ObjectModelFactoryOpc : InstanceModelFactoryOpc<ObjectModel, ObjectTypeModel, ObjectTypeModelFactoryOpc>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            if (opcNode is BaseObjectState objState)
            {
                _model.EventNotifier = objState.EventNotifier;
            }
        }
    }

    public class BaseTypeModelFactoryOpc<TBaseTypeModel> : NodeModelFactoryOpc<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            var uaType = opcNode as BaseTypeState;

            if (uaType.SuperTypeId != null)
            {
                var superTypeNodeId = opcContext.GetModelNodeId(uaType.SuperTypeId);
                BaseTypeModel superTypeModel = opcContext.GetModelForNode<TBaseTypeModel>(superTypeNodeId);
                if (superTypeModel == null)
                {
                    // Handle cases where the supertype is of a different model class, for example the InterfaceModel for BaseInterfaceType has a supertype ObjectTypeModel, while all other InterfaceModels have a supertype of Interfacemodel
                    superTypeModel = opcContext.GetModelForNode<BaseTypeModel>(superTypeNodeId);
                }
                if (superTypeModel == null)
                {
                    var superTypeState = opcContext.GetNode(uaType.SuperTypeId) as BaseTypeState;
                    if (superTypeState != null)
                    {
                        // Always resolve basetypes, regardless of recursionDepth
                        superTypeModel = NodeModelFactoryOpc.Create(opcContext, superTypeState, this._model.CustomState, out _, 2) as BaseTypeModel;
                        if (superTypeModel == null)
                        {
                            throw new Exception($"Invalid node {superTypeState} is not a Base Type");
                        }
                    }
                }
                _model.SuperType = superTypeModel;
                _model.RemoveInheritedAttributes(_model.SuperType);
                foreach (var uaInterface in _model.Interfaces)
                {
                    _model.RemoveInheritedAttributes(uaInterface);
                }
            }
            else
            {
                _model.SuperType = null;
            }
            _model.IsAbstract = uaType.IsAbstract;
        }

    }

    public class ObjectTypeModelFactoryOpc<TTypeModel> : BaseTypeModelFactoryOpc<TTypeModel> where TTypeModel : BaseTypeModel, new()
    {
    }

    public class ObjectTypeModelFactoryOpc : ObjectTypeModelFactoryOpc<ObjectTypeModel>
    {
    }

    public class InterfaceModelFactoryOpc : ObjectTypeModelFactoryOpc<InterfaceModel>
    {
    }

    public class VariableModelFactoryOpc<TVariableModel> : InstanceModelFactoryOpc<TVariableModel, VariableTypeModel, VariableTypeModelFactoryOpc>
        where TVariableModel : VariableModel, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            var variableNode = opcNode as BaseVariableState;

            InitializeDataTypeInfo(_model, opcContext, variableNode, recursionDepth);
            if (variableNode.AccessLevelEx != 1) _model.AccessLevel = variableNode.AccessLevelEx;
            // deprecated if (variableNode.UserAccessLevel != 1) _model.UserAccessLevel = variableNode.UserAccessLevel;
            if (variableNode.AccessRestrictions != 0) _model.AccessRestrictions = (ushort)variableNode.AccessRestrictions;
            if (variableNode.WriteMask != 0) _model.WriteMask = (uint)variableNode.WriteMask;
            if (variableNode.UserWriteMask != 0) _model.UserWriteMask = (uint)variableNode.UserWriteMask;
            if (variableNode.MinimumSamplingInterval != 0)
            {
                _model.MinimumSamplingInterval = variableNode.MinimumSamplingInterval;
            }

            var invalidBrowseNameOnTypeInformation = _model.Properties.Where(p =>
                    (p.BrowseName.EndsWith(BrowseNames.EnumValues) && p.BrowseName != opcContext.GetModelBrowseName(BrowseNames.EnumValues))
                || (p.BrowseName.EndsWith(BrowseNames.EnumStrings) && p.BrowseName != opcContext.GetModelBrowseName(BrowseNames.EnumStrings))
                || (p.BrowseName.EndsWith(BrowseNames.OptionSetValues) && p.BrowseName != opcContext.GetModelBrowseName(BrowseNames.OptionSetValues))
            );
            if (invalidBrowseNameOnTypeInformation.Any())
            {
                opcContext.Logger.LogWarning($"Found type definition node with browsename in non-default namespace: {string.Join("", invalidBrowseNameOnTypeInformation.Select(ti => ti.BrowseName))}");
            }


            if (string.IsNullOrEmpty(this._model.NodeSet.XmlSchemaUri) && variableNode.TypeDefinitionId == VariableTypeIds.DataTypeDictionaryType)
            {
                var namespaceUriModelBrowseName = opcContext.GetModelBrowseName(BrowseNames.NamespaceUri);
                var xmlNamespaceVariable = _model.Properties.FirstOrDefault(dv => dv.BrowseName == namespaceUriModelBrowseName);
                if (_model.Parent.NodeId == opcContext.GetModelNodeId(ObjectIds.XmlSchema_TypeSystem))
                {
                    if (xmlNamespaceVariable != null && !string.IsNullOrEmpty(xmlNamespaceVariable.Value))
                    {
                        var variant = opcContext.JsonDecodeVariant(xmlNamespaceVariable.Value);
                        var namespaceUri = variant.Value as string;
                        if (!string.IsNullOrEmpty(namespaceUri))
                        {
                            this._model.NodeSet.XmlSchemaUri = namespaceUri;
                        }
                    }
                }
            }
        }

        internal static void InitializeDataTypeInfo(VariableModel _model, IOpcUaContext opcContext, BaseVariableState variableNode, int recursionDepth)
        {
            VariableTypeModelFactoryOpc.InitializeDataTypeInfo(_model, opcContext, $"{variableNode.GetType()} {variableNode}", variableNode.DataType, variableNode.ValueRank, variableNode.ArrayDimensions, variableNode.WrappedValue, recursionDepth);
        }
    }

    public class DataVariableModelFactoryOpc : VariableModelFactoryOpc<DataVariableModel>
    {
    }

    public class PropertyModelFactoryOpc : VariableModelFactoryOpc<PropertyModel>
    {
    }

    public class MethodModelFactoryOpc : InstanceModelFactoryOpc<MethodModel, MethodModel, MethodModelFactoryOpc> // TODO determine if intermediate base classes of MethodState are worth exposing in the model
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            if (opcNode is MethodState methodState)
            {
                var references = opcContext.GetHierarchyReferences(methodState);
                foreach (var reference in references.Where(r => r.ReferenceTypeId == ReferenceTypeIds.HasProperty))
                {
                    var referencedNode = opcContext.GetNode(reference.TargetId);
                    if (referencedNode?.BrowseName == "InputArguments" || referencedNode?.BrowseName == "OutputArguments")
                    {
                        if (referencedNode is PropertyState argumentProp && argumentProp.Value is ExtensionObject[] arguments)
                        {
                            var argumentInfo = new PropertyState<Argument[]>(methodState)
                            {
                                NodeId = argumentProp.NodeId,
                                TypeDefinitionId = argumentProp.TypeDefinitionId,
                                ModellingRuleId = argumentProp.ModellingRuleId,
                                DataType = argumentProp.DataType,
                            };
                            argumentInfo.Value = new Argument[arguments.Length];
                            for (int arg = 0; arg < arguments.Length; arg++)
                            {
                                argumentInfo.Value[arg] = arguments[arg].Body as Argument;
                            }
                            if (referencedNode?.BrowseName == "InputArguments")
                            {
                                methodState.InputArguments = argumentInfo;
                            }
                            else
                            {
                                methodState.OutputArguments = argumentInfo;
                            }
                        }
                    }
                }

                //_model.MethodDeclarationId = opcContext.GetNodeIdWithUri(methodState.MethodDeclarationId, out var _);
                var inputArgsModelBrowseName = opcContext.GetModelBrowseName(BrowseNames.InputArguments);
                var inputArgs = _model.Properties.FirstOrDefault(p => p.BrowseName == inputArgsModelBrowseName);
                if (inputArgs != null)
                {
                    _model.InputArguments = new List<VariableModel>();
                    ProcessMethodArguments(_model, BrowseNames.InputArguments, inputArgs, _model.InputArguments, opcContext, recursionDepth);
                }
                var outputArgsModelBrowseName = opcContext.GetModelBrowseName(BrowseNames.OutputArguments);
                var outputArgs = _model.Properties.FirstOrDefault(p => p.BrowseName == outputArgsModelBrowseName);
                if (outputArgs != null)
                {
                    _model.OutputArguments = new List<VariableModel>();
                    ProcessMethodArguments(_model, BrowseNames.OutputArguments, outputArgs, _model.OutputArguments, opcContext, recursionDepth);
                }
            }
            else
            {
                throw new Exception($"Unexpected node type for method {opcNode}");
            }
        }

        private void ProcessMethodArguments(MethodModel methodModel, string browseName, VariableModel argumentVariable, List<VariableModel> modelArguments, IOpcUaContext opcContext, int recursionDepth)
        {
            var arguments = opcContext.JsonDecodeVariant(argumentVariable.Value, argumentVariable.DataType); // TODO get from opcContext!
            if (arguments.Value != null)
            {
                foreach (var argObj in arguments.Value as Array)
                {
                    var arg = (argObj as ExtensionObject)?.Body as Argument;

                    var dataTypeStateObj = opcContext.GetNode(arg.DataType);
                    if (dataTypeStateObj is DataTypeState dataTypeState)
                    {
                        var dataType = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataTypeState, null, recursionDepth);

                        var argumentDescription = _model.OtherReferencedNodes
                            .FirstOrDefault(nr => nr.Node.GetUnqualifiedBrowseName() == arg.Name
                                && ((nr.ReferenceType as ReferenceTypeModel).HasBaseType($"{Namespaces.OpcUa};{ReferenceTypeIds.HasArgumentDescription}")
                                    || (nr.ReferenceType as ReferenceTypeModel).HasBaseType($"{Namespaces.OpcUa};{ReferenceTypeIds.HasOptionalInputArgumentDescription}"))
                                );
                        var argumentModel = argumentDescription?.Node as VariableModel;
                        if (argumentModel == null)
                        {
                            // No description: create an argument variable
                            argumentModel = new VariableModel
                            {
                                DisplayName = new List<NodeModel.LocalizedText> { new NodeModel.LocalizedText { Text = arg.Name } },
                                BrowseName = arg.Name,
                                Description = arg.Description?.ToModel(),
                                NodeSet = argumentVariable.NodeSet,
                                NodeId = argumentVariable.NodeId,
                                CustomState = argumentVariable.CustomState,
                            };
                            VariableTypeModelFactoryOpc.InitializeDataTypeInfo(argumentModel, opcContext, $"Method {_model} Argument {arg.Name}", arg.DataType, arg.ValueRank, new ReadOnlyList<uint>(arg.ArrayDimensions, false), new Variant(arg.Value), recursionDepth);
                        }
                        else
                        {
                            // TODO validate variable against argument property
                            if ((argumentDescription.ReferenceType as ReferenceTypeModel).HasBaseType($"{Namespaces.OpcUa};{ReferenceTypeIds.HasOptionalInputArgumentDescription}"))
                            {
                                argumentModel.ModellingRule = "Optional";
                            }
                            else
                            {
                                argumentModel.ModellingRule = "Mandatory";
                            }
                        }
                        modelArguments.Add(argumentModel);
                    }
                    else
                    {
                        throw new Exception($"Invalid data type {arg.DataType} for argument {arg.Name} in method {_model.NodeId}.");
                    }
                }
            }
        
            //if (argumentVariable.OtherReferencedNodes?.Any() != true && argumentVariable.OtherReferencingNodes?.Any() != true)
            //{
            //    var argumentProperty = NodeModelOpcExtensions.GetArgumentProperty(methodModel, browseName, modelArguments, opcContext);
            //}
            //else
            //{

            //}
        }
    }

    public class VariableTypeModelFactoryOpc : BaseTypeModelFactoryOpc<VariableTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            var variableTypeState = opcNode as BaseVariableTypeState;
            InitializeDataTypeInfo(_model, opcContext, variableTypeState, recursionDepth);
            //variableTypeState.ValueRank
            //variableTypeState.Value
            //variableTypeState.ArrayDimensions
            //_model.
        }

        internal static void InitializeDataTypeInfo(VariableTypeModel model, IOpcUaContext opcContext, BaseVariableTypeState variableTypeNode, int recursionDepth)
        {
            VariableTypeModelFactoryOpc.InitializeDataTypeInfo(model, opcContext, $"{variableTypeNode.GetType()} {variableTypeNode}", variableTypeNode.DataType, variableTypeNode.ValueRank, variableTypeNode.ArrayDimensions, variableTypeNode.WrappedValue, recursionDepth);
        }

        internal static void InitializeDataTypeInfo(IVariableDataTypeInfo model, IOpcUaContext opcContext, string variableNodeDiagInfo, NodeId dataTypeNodeId, int valueRank, ReadOnlyList<uint> arrayDimensions, Variant wrappedValue, int recursionDepth)
        {
            var dataType = opcContext.GetNode(dataTypeNodeId);
            if (dataType is DataTypeState)
            {
                model.DataType = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType as DataTypeState, null, recursionDepth);
            }
            else
            {
                if (dataType == null)
                {
                    throw new Exception($"{variableNodeDiagInfo}: did not find data type {dataTypeNodeId} (Namespace {opcContext.NamespaceUris.GetString(dataTypeNodeId.NamespaceIndex)}).");
                }
                else
                {
                    throw new Exception($"{variableNodeDiagInfo}: Unexpected node state {dataTypeNodeId}/{dataType?.GetType().FullName}.");
                }
            }
            if (valueRank != -1)
            {
                model.ValueRank = valueRank;
                if (arrayDimensions != null && arrayDimensions.Any())
                {
                    model.ArrayDimensions = String.Join(",", arrayDimensions);
                }
            }
            if (wrappedValue.Value != null)
            {
                var encodedValue = opcContext.JsonEncodeVariant(wrappedValue, model.DataType);
                model.Value = encodedValue.Json;
            }
        }

    }
    public class DataTypeModelFactoryOpc : BaseTypeModelFactoryOpc<DataTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);

            var dataTypeState = opcNode as DataTypeState;
            if (dataTypeState.DataTypeDefinition?.Body != null)
            {
                var sd = dataTypeState.DataTypeDefinition.Body as StructureDefinition;
                if (sd != null)
                {
                    _model.StructureFields = new List<DataTypeModel.StructureField>();
                    int order = 0;
                    // The OPC SDK does not put the SymbolicName into the node state: read from UANodeSet
                    var uaNodeSet = opcContext.GetUANodeSet(_model.Namespace);
                    UADataType uaStruct = null;
                    if (uaNodeSet != null)
                    {
                        var opcNodeIdStr = opcNode.NodeId.ToString();
                        uaStruct = uaNodeSet.Items.FirstOrDefault(n => n.NodeId == opcNodeIdStr) as UADataType;
                    }

                    foreach (var field in sd.Fields)
                    {
                        var dataType = opcContext.GetNode(field.DataType);
                        if (dataType is DataTypeState)
                        {
                            var dataTypeModel = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType as DataTypeState, null, recursionDepth);
                            if (dataTypeModel == null)
                            {
                                throw new Exception($"Unable to resolve data type {dataType.DisplayName}");
                            }
                            string symbolicName = null;
                            if (uaStruct != null)
                            {
                                symbolicName = uaStruct?.Definition?.Field?.FirstOrDefault(f => f.Name == field.Name)?.SymbolicName;
                            }
                            var structureField = new DataTypeModel.StructureField
                            {
                                Name = field.Name,
                                SymbolicName = symbolicName,
                                DataType = dataTypeModel,
                                ValueRank = field.ValueRank != -1 ? field.ValueRank : null,
                                ArrayDimensions = field.ArrayDimensions != null && field.ArrayDimensions.Any() ? String.Join(",", field.ArrayDimensions) : null,
                                MaxStringLength = field.MaxStringLength != 0 ? field.MaxStringLength : null,
                                Description = field.Description.ToModel(),
                                IsOptional = field.IsOptional && sd.StructureType == StructureType.StructureWithOptionalFields,
                                AllowSubTypes = field.IsOptional && (sd.StructureType == StructureType.StructureWithSubtypedValues || sd.StructureType == StructureType.UnionWithSubtypedValues),
                                FieldOrder = order++,
                            };
                            _model.StructureFields.Add(structureField);
                        }
                        else
                        {
                            if (dataType == null)
                            {
                                throw new Exception($"Unable to find node state for data type {field.DataType} in {opcNode}");
                            }
                            throw new Exception($"Unexpected node state {dataType?.GetType()?.FullName} for data type {field.DataType} in {opcNode}");
                        }
                    }
                }
                else
                {
                    var enumFields = dataTypeState.DataTypeDefinition.Body as EnumDefinition;
                    if (enumFields != null)
                    {
                        _model.IsOptionSet = enumFields.IsOptionSet || _model.HasBaseType(opcContext.GetModelNodeId(DataTypeIds.OptionSet));
                        _model.EnumFields = new List<DataTypeModel.UaEnumField>();

                        // The OPC SDK does not put the SymbolicName into the node state: read from UANodeSet
                        var uaNodeSet = opcContext.GetUANodeSet(_model.Namespace);
                        UADataType uaEnum = null;
                        if (uaNodeSet != null)
                        {
                            uaEnum = uaNodeSet.Items.FirstOrDefault(n => n.NodeId == opcNode.NodeId) as UADataType;
                        }
                        foreach (var field in enumFields.Fields)
                        {
                            string symbolicName = null;
                            if (uaEnum != null)
                            {
                                symbolicName = uaEnum?.Definition?.Field?.FirstOrDefault(f => f.Name == field.Name)?.SymbolicName;
                            }
                            var enumField = new DataTypeModel.UaEnumField
                            {
                                Name = field.Name,
                                DisplayName = field.DisplayName.ToModel(),
                                Value = field.Value,
                                Description = field.Description.ToModel(),
                                SymbolicName = symbolicName,
                            };
                            _model.EnumFields.Add(enumField);
                        }
                    }
                    else
                    {
                        throw new Exception($"Unknown data type definition in {dataTypeState}");
                    }
                }
            }
        }
    }

    public class ReferenceTypeModelFactoryOpc : BaseTypeModelFactoryOpc<ReferenceTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode, int recursionDepth)
        {
            base.Initialize(opcContext, opcNode, recursionDepth);
            var referenceTypeState = opcNode as ReferenceTypeState;

            _model.InverseName = referenceTypeState.InverseName?.ToModel();
            _model.Symmetric = referenceTypeState.Symmetric;
        }
    }
}

namespace CESMII.OpcUa.NodeSetModel
{

    public static class LocalizedTextExtension
    {
        public static NodeModel.LocalizedText ToModelSingle(this ua.LocalizedText text) => text != null ? new NodeModel.LocalizedText { Text = text.Text, Locale = text.Locale } : null;
        public static List<NodeModel.LocalizedText> ToModel(this ua.LocalizedText text) => text != null ? new List<NodeModel.LocalizedText> { text.ToModelSingle() } : new List<NodeModel.LocalizedText>();
        public static List<NodeModel.LocalizedText> ToModel(this IEnumerable<ua.LocalizedText> texts) => texts?.Select(text => text.ToModelSingle()).Where(lt => lt != null).ToList();
    }
}
