using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CESMII.OpcUa.NodeSetModel
{
    public class NodeSetModel
    {
        public string ModelUri { get; set; }
        public string Version { get; set; }
        public DateTime? PublicationDate { get; set; }
        // RequiredModels
        // NamespaceUris
        // ServerUris
        // DefaultAccessRules

        public override string ToString() => $"{ModelUri} {Version} ({PublicationDate})";

        // SequenceNumber/IsUpdate
        public object CustomState { get; set; }

        /// <summary>
        /// This is equivalent to ProfileItem of type Class.
        /// </summary>
        public virtual List<ObjectTypeModel> ObjectTypes { get; set; } = new List<ObjectTypeModel>();

        /// <summary>
        /// This is equivalent to ProfileItem of type CustomDataType.
        /// This is used primarily to represent structure type objects (ie MessageCode example). 
        /// This is typically not an instance. 
        /// This could have its own complex properties associated with it.
        /// </summary>
        public virtual List<VariableTypeModel> VariableTypes { get; set; } = new List<VariableTypeModel>();

        /// <summary>
        /// No clean mapping yet.
        /// TBD - Lower priority.
        /// appears to be enumerations
        /// more globally re-usable
        /// TBD - ask JW if we can only use these to be selected and not allow building them out.
        /// </summary>
        public virtual List<DataTypeModel> DataTypes { get; set; } = new List<DataTypeModel>();

        /// <summary>
        /// This is equivalent to ProfileInterface.
        /// </summary>
        public virtual List<InterfaceModel> Interfaces { get; set; } = new List<InterfaceModel>();
        public virtual List<ObjectModel> Objects { get; set; } = new List<ObjectModel>();

        public virtual List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public virtual List<DataVariableModel> DataVariables { get; set; } = new List<DataVariableModel>();

        public virtual List<NodeModel> UnknownNodes { get; set; } = new List<NodeModel>();
        
        public Dictionary<string, NodeModel> AllNodes = new Dictionary<string, NodeModel>();

        public void UpdateAllNodes()
        {
            AllNodes.Clear();
            foreach (var dataType in DataTypes)
            {
                if (!AllNodes.TryAdd(/*NodeId.Parse*/(dataType.NodeId), dataType))
                {
                    // Duplicate node id!
                }
            }
            foreach (var variableType in VariableTypes)
            {
                if (!AllNodes.TryAdd(/*NodeId.Parse*/(variableType.NodeId), variableType))
                {

                }
            }
            foreach (var uaInterface in Interfaces)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(uaInterface.NodeId), uaInterface);
            }
            foreach (var objectType in ObjectTypes)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(objectType.NodeId), objectType);
            }
            foreach (var uaObject in Objects)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(uaObject.NodeId), uaObject);
            }
            foreach (var property in Properties)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(property.NodeId), property);
            }
            foreach (var dataVariable in DataVariables)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(dataVariable.NodeId), dataVariable);
            }
            foreach (var node in UnknownNodes)
            {
                AllNodes.TryAdd(/*NodeId.Parse*/(node.NodeId), node);
            }
        }

        public void UpdateIndices()
        {
            AllNodes.Clear();
            var updatedNodes = new List<NodeModel>();
            foreach (var dataType in DataTypes)
            {
                dataType.UpdateIndices(this, updatedNodes);
            }
            foreach (var variableType in VariableTypes)
            {
                variableType.UpdateIndices(this, updatedNodes);
            }
            foreach (var uaInterface in Interfaces)
            {
                uaInterface.UpdateIndices(this, updatedNodes);
            }
            foreach (var objectType in ObjectTypes)
            {
                objectType.UpdateIndices(this, updatedNodes);
            }
            foreach (var property in Properties)
            {
                property.UpdateIndices(this, updatedNodes);
            }
            foreach (var dataVariable in DataVariables)
            {
                dataVariable.UpdateIndices(this, updatedNodes);
            }
            foreach (var uaObject in Objects)
            {
                uaObject.UpdateIndices(this, updatedNodes);
            }
            foreach (var node in UnknownNodes)
            {
                node.UpdateIndices(this, updatedNodes);
            }
        }
    }

    public class NodeModel
    {
        public virtual List<LocalizedText> DisplayName { get; set; }
        public string BrowseName { get; set; }
        public string SymbolicName { get; set; }
        public string GetBrowseName()
        {
            return BrowseName ?? $"{Namespace}:{DisplayName}";
        }

        public virtual List<LocalizedText> Description { get; set; }
        public string Documentation { get; set; }
        public string Namespace { get; set; }
        public string NodeId { get; set; }
        public object CustomState { get; set; }
        public virtual List<string> Categories { get; set; }

        public virtual NodeSetModel NodeSet { get; set; }

        public class LocalizedText
        {
            public string Text { get; set; }
            public string Locale { get; set; }

            public static implicit operator LocalizedText(string text) => new LocalizedText { Text = text };
            public static List<LocalizedText> ListFromText (string text) => text != null ? new List<LocalizedText> { new LocalizedText { Text = text } } : new List<LocalizedText>();
            public override string ToString() => Text;
        }

        /// <summary>
        /// This is equivalent to ProfileAttribute except it keeps compositions, variable types elsewhere.
        /// Relatively static (ie. serial number.) over the life of the object instance
        /// </summary>
        public virtual List<VariableModel> Properties { get; set; } = new List<VariableModel>(); // Surprisingly, properties can also be of type DataVariable - need to allow both by using the common base class

        /// <summary>
        /// This is equivalent to ProfileAttribute. More akin to variable types.
        /// More dynamic (ie. RPM, temperature.)
        /// TBD - figure out a way to distinguish between data variables and properties.  
        /// </summary>
        public virtual List<DataVariableModel> DataVariables { get; set; } = new List<DataVariableModel>();

        /// <summary>
        /// This is equivalent to ProfileAttribute.
        /// Sub-systems. More akin to compositions.
        /// (ie. starter, control unit). Complete sub-system that could be used by other profiles. 
        /// The object model has name, description and then ObjectType much like Profile attribute has name, description, Composition(Id). 
        /// </summary>
        public virtual List<ObjectModel> Objects { get; set; } = new List<ObjectModel>();

        /// <summary>
        /// This is equivalent to Interfaces in ProfileItem.
        /// If someone implemented an interface, the objectType should dynamically "get" those properties. 
        /// Essentially, the implementing objectType does not hardcode the properties of the interface. any change to the
        /// interface would automatically be shown to the user on the implementing objectTypes.
        /// </summary>
        public virtual List<InterfaceModel> Interfaces { get; set; } = new List<InterfaceModel>();

        /// <summary>
        /// TBD - defer for now
        /// </summary>
        public virtual List<MethodModel> Methods { get; set; } = new List<MethodModel>();
        /// <summary>
        /// TBD - defer for now
        /// </summary>
        public virtual List<ObjectTypeModel> Events { get; set; } = new List<ObjectTypeModel>();

        public class ChildAndReference
        {
            public virtual NodeModel Child { get; set; }
            public string Reference { get; set; }
        }

        public virtual List<ChildAndReference> OtherChilden { get; set; } = new List<ChildAndReference>();

        public virtual bool UpdateIndices(NodeSetModel model, List<NodeModel> updatedNodes)
        {
            if (updatedNodes.Contains(this))
            {
                // break some recursions
                return false;
            }
            updatedNodes.Add(this);
            if (model.ModelUri == this.Namespace)
            {
                model.AllNodes.TryAdd(this.NodeId, this);
            }
            foreach (var node in Objects)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            foreach (var node in this.DataVariables)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            foreach (var node in this.Interfaces)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            foreach (var node in this.Methods)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            foreach (var node in this.Properties)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            foreach (var node in this.Events)
            {
                node.UpdateIndices(model, updatedNodes);
            }
            return true;
        }

        public override string ToString()
        {
            return $"{DisplayName?.FirstOrDefault()} ({Namespace}: {NodeId})";
        }
    }

    public abstract class InstanceModelBase : NodeModel
    {
        /// <summary>
        /// Values: Optional, Mandatory, MandatoryPlaceholder, OptionalPlaceholder, ExposesItsArray
        /// </summary>
        public string ModelingRule { get; set; }
        public virtual NodeModel Parent
        {
            get => _parent;
            set
            {
                if (Parent != null && Parent != value)
                {
                    return;
                    //throw new Exception($"Changing parent or multiple parents on {this}: new parent {value}, previous parent {_parent}");
                }
                _parent = value;
            }
        }
        private NodeModel _parent;
    }
    public abstract class InstanceModel<TTypeDefinition> : InstanceModelBase where TTypeDefinition : BaseTypeModel, new()
    {
        public virtual TTypeDefinition TypeDefinition { get; set; }
    }

    public class ObjectModel : InstanceModel<ObjectTypeModel>
    {
    }

    public class BaseTypeModel : NodeModel
    {
        public bool IsAbstract { get; set; }
        /// <summary>
        /// This is equivalent to ProfileItem.Parent.
        /// </summary>
        public virtual BaseTypeModel SuperType { get; set; }
        /// <summary>
        /// This is equivalent to ProfileItem.Children.
        /// Dynamically assembled from list of types ids...
        /// Not serialized.
        /// </summary>
        [IgnoreDataMember] // This can contain cycle (and is easily recreated from the SubTypeId)
        public virtual List<BaseTypeModel> SubTypes { get; set; } = new List<BaseTypeModel>();

        public bool HasBaseType(string nodeId)
        {
            var baseType = this;
            do
            {
                if (baseType?.NodeId == nodeId)
                {
                    return true;
                }
                baseType = baseType.SuperType;
            }
            while (baseType != null);
            return false;
        }

        public void RemoveInheritedAttributes(BaseTypeModel superTypeModel)
        {
            while (superTypeModel != null)
            {
                RemoveByBrowseName(Properties, superTypeModel.Properties);
                RemoveByBrowseName(DataVariables, superTypeModel.DataVariables);
                RemoveByBrowseName(Objects, superTypeModel.Objects);
                RemoveByBrowseName(Interfaces, superTypeModel.Interfaces);
                foreach (var uaInterface in superTypeModel.Interfaces)
                {
                    RemoveInheritedAttributes(uaInterface);
                }
                RemoveByBrowseName(Methods, superTypeModel.Methods);
                RemoveByBrowseName(Events, superTypeModel.Events);

                superTypeModel = superTypeModel?.SuperType;
            };
        }

        private void RemoveByBrowseName<T>(List<T> properties, List<T> propertiesToRemove) where T : NodeModel
        {
            foreach (var property in propertiesToRemove)
            {
                if (properties.RemoveAll(p => p.GetBrowseName() == property.GetBrowseName()
                    && p.NodeId == property.NodeId
                    ) > 0)
                {
                }
            }
        }

        public override bool UpdateIndices(NodeSetModel model, List<NodeModel> updatedNodes)
        {
            var bUpdated = base.UpdateIndices(model, updatedNodes);
            if (bUpdated)
            {
                if (SuperType != null)
                {
                    if (!SuperType.SubTypes.Any(sub => sub.NodeId == this.NodeId))
                    {
                        SuperType.SubTypes.Add(this);
                    }
                    else
                    {

                    }
                }
            }
            return bUpdated;
        }

    }

    public class ObjectTypeModel : BaseTypeModel
    {
    }

    public class InterfaceModel : ObjectTypeModel
    {
    }

    public class VariableModel : InstanceModel<VariableTypeModel>
    {
        public virtual BaseTypeModel DataType { get; set; }
        /// <summary>
        /// n > 1: the Value is an array with the specified number of dimensions.
        /// OneDimension(1) : The value is an array with one dimension.
        /// OneOrMoreDimensions(0): The value is an array with one or more dimensions.
        /// Scalar(−1): The value is not an array.
        /// Any(−2): The value can be a scalar or an array with any number of dimensions.
        /// ScalarOrOneDimension(−3): The value can be a scalar or a one dimensional array.
        /// </summary>
        public int? ValueRank { get; set; }
        /// <summary>
        /// Comma separated list
        /// </summary>
        public string ArrayDimensions { get; set; }
        public string Value { get; set; }

        // Engineering units:
        public class EngineeringUnitInfo
        {
            public LocalizedText DisplayName { get; set; }
            public LocalizedText Description { get; set; }
            public string NamespaceUri { get; set; }
            public int? UnitId { get; set; }
        }

        virtual public EngineeringUnitInfo EngineeringUnit { get; set; }
        public string EngUnitNodeId { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public double? InstrumentMinValue { get; set; }
        public double? InstrumentMaxValue { get; set; }
        public long? EnumValue { get; set; }

        public uint? AccessLevel { get; set; }
        public uint? UserAccessLevel { get; set; }
        public ushort? AccessRestrictions { get; set; }
        public uint? WriteMask { get; set; }
        public uint? UserWriteMask { get; set; }
    }

    public class DataVariableModel : VariableModel
    {
    }

    public class PropertyModel : VariableModel
    {
    }

    public class MethodModel : InstanceModel<BaseTypeModel>
    {
    }

    public class VariableTypeModel : BaseTypeModel
    {
    }

    public class DataTypeModel : BaseTypeModel
    {
        public virtual List<StructureField> StructureFields { get; set; }
        public virtual List<UaEnumField> EnumFields { get; set; }

        public class StructureField
        {
            public string Name { get; set; }
            public virtual BaseTypeModel DataType { get; set; }
            public virtual List<LocalizedText> Description { get; set; }
            public bool IsOptional { get; set; }
            public override string ToString() => $"{Name}: {DataType} {(IsOptional ? "Optional" : "")}";

        }

        public class UaEnumField
        {
            public string Name { get; set; }
            public virtual List<LocalizedText> DisplayName { get; set; }
            public virtual List<LocalizedText> Description { get; set; }
            public long Value {get; set; }

            public override string ToString() => $"{Name} = {Value}";
        }

        public override bool UpdateIndices(NodeSetModel model, List<NodeModel> updatedNodes)
        {
            var bUpdated = base.UpdateIndices(model, updatedNodes);
            if (bUpdated)
            {
                if (StructureFields?.Any() == true)
                {
                    foreach (var field in StructureFields)
                    {
                        field.DataType?.UpdateIndices(model, updatedNodes);
                    }
                }
            }
            return bUpdated;
        }

    }

#if NETSTANDARD2_0
    static class DictExtensions
    {
        public static bool TryAdd(this Dictionary<string, NodeModel> dict, string key, NodeModel value)
        {
            if (dict.ContainsKey(key)) return false;
            dict.Add(key, value);
            return true;
        }
    }
#endif
}