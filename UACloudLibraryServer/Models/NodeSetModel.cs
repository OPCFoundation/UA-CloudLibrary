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
using System.Runtime.Serialization;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class NodeSetModel
    {
        public virtual NamespaceMetaDataModel Metadata { get; set; }

        public string ModelUri { get; set; }

        public string Version { get; set; }

        public DateTime? PublicationDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        public string XmlSchemaUri { get; set; }

        // RequiredModels
        public virtual List<RequiredModelInfoModel> RequiredModels { get; set; } = new List<RequiredModelInfoModel>();
        // NamespaceUris
        // ServerUris
        // DefaultAccessRules

        public override string ToString() => $"{ModelUri} {Version} ({PublicationDate})";

        /// <summary>
        /// Unique identifier for this nodeset, optionally assigned by the managing application. Not used in the nodeset model classes
        /// </summary>
        public string Identifier { get; set; }

        // For use by the application
        public object CustomState { get; set; }

        /// <summary>
        /// The UA object types defined by this node set
        /// </summary>
        public virtual List<ObjectTypeModel> ObjectTypes { get; set; } = new List<ObjectTypeModel>();

        /// <summary>
        /// The UA variable types defined by this node set
        /// </summary>
        public virtual List<VariableTypeModel> VariableTypes { get; set; } = new List<VariableTypeModel>();

        /// <summary>
        /// The UA data types defined by this node set
        /// </summary>
        public virtual List<DataTypeModel> DataTypes { get; set; } = new List<DataTypeModel>();

        /// <summary>
        /// The UA interfaces defined by this node set
        /// </summary>
        public virtual List<InterfaceModel> Interfaces { get; set; } = new List<InterfaceModel>();

        public virtual List<ObjectModel> Objects { get; set; } = new List<ObjectModel>();

        public virtual List<MethodModel> Methods { get; set; } = new();

        public virtual List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();

        public virtual List<DataVariableModel> DataVariables { get; set; } = new List<DataVariableModel>();

        public virtual List<NodeModel> UnknownNodes { get; set; } = new List<NodeModel>();

        public virtual List<ReferenceTypeModel> ReferenceTypes { get; set; } = new List<ReferenceTypeModel>();

        public Dictionary<string, NodeModel> AllNodesByNodeId { get; } = new Dictionary<string, NodeModel>();

        public string HeaderComments { get; set; }

        public int? NamespaceIndex { get; set; }
    }

    public class RequiredModelInfoModel
    {
        public string ModelUri { get; set; }

        public string Version { get; set; }

        public DateTime? PublicationDate { get; set; }

        virtual public NodeSetModel AvailableModel { get; set; }
    }

    public class NodeModel
    {
        public virtual List<LocalizedText> DisplayName { get; set; }

        public string BrowseName { get; set; }

        public string SymbolicName { get; set; }

        public string GetBrowseName()
        {
            return BrowseName ?? $"{Namespace}:{DisplayName?.FirstOrDefault()?.Text}";
        }

        public virtual List<LocalizedText> Description { get; set; }

        public string Documentation { get; set; }

        /// <summary>
        /// Released, Draft, Deprecated
        /// </summary>
        public string ReleaseStatus { get; set; }

        [IgnoreDataMember]
        public string Namespace { get => NodeSet?.ModelUri; }

        public string NodeId { get; set; }

        public string NodeIdIdentifier { get; set; }

        public IEnumerable<NodeAndReference> AllReferencedNodes { get; set; }

        public virtual NodeSetModel NodeSet { get; set; }

        public class LocalizedText
        {
            public LocalizedText()
            {
                Text = "";
            }

#nullable enable
            public string Text { get => _text; set => _text = value ?? ""; }

            private string _text;
#nullable restore

            public string Locale { get; set; }

            public static implicit operator LocalizedText(string text) => text == null ? null : new LocalizedText { Text = text };

            public static List<LocalizedText> ListFromText(string text) => text != null ? new List<LocalizedText> { new LocalizedText { Text = text } } : new List<LocalizedText>();

            public override string ToString() => Text;
        }

        /// <summary>
        /// OPC UA: HasProperty references
        /// </summary>
        public virtual List<VariableModel> Properties { get; set; } = new List<VariableModel>(); // Surprisingly, properties can also be of type DataVariable - need to allow both by using the common base class

        /// <summary>
        /// OPC UA: HasComponent references (or of derived reference type) to a DataVariable
        /// </summary>
        public virtual List<DataVariableModel> DataVariables { get; set; } = new List<DataVariableModel>();

        /// <summary>
        /// OPC UA: HasComponent references (or of derived reference types) to an Object
        /// </summary>
        public virtual List<ObjectModel> Objects { get; set; } = new List<ObjectModel>();

        /// <summary>
        /// OPC UA: HasInterface references (or of derivce reference types)
        /// </summary>
        public virtual List<InterfaceModel> Interfaces { get; set; } = new List<InterfaceModel>();

        /// <summary>
        /// TBD - defer for now
        /// OPC UA: HasComponent references (or of derived reference types) to a MethodType
        /// </summary>
        public virtual List<MethodModel> Methods { get; set; } = new List<MethodModel>();

        /// <summary>
        /// OPC UA: GeneratesEvent references (or of derived reference types)
        /// </summary>
        public virtual List<ObjectTypeModel> Events { get; set; } = new List<ObjectTypeModel>();

        public class NodeAndReference : IEquatable<NodeAndReference>
        {
            public virtual NodeModel Node { get; set; }

            public virtual NodeModel ReferenceType { get; set; }

            public override bool Equals(object obj)
            {
                return Equals(obj as NodeAndReference);
            }

            public bool Equals(NodeAndReference other)
            {
                return other is not null &&
                       EqualityComparer<NodeModel>.Default.Equals(Node, other.Node) &&
                       EqualityComparer<NodeModel>.Default.Equals(ReferenceType, other.ReferenceType);
            }

            public override int GetHashCode()
            {
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                return HashCode.Combine(Node, ReferenceType);
#else
                return HashCode.Combine(Node, ReferenceType, "");
#endif
            }

            public static bool operator ==(NodeAndReference left, NodeAndReference right)
            {
                return EqualityComparer<NodeAndReference>.Default.Equals(left, right);
            }

            public static bool operator !=(NodeAndReference left, NodeAndReference right)
            {
                return !(left == right);
            }

            public override string ToString()
            {
                return $"{ReferenceType?.ToString()} {Node}";
            }
        }

        public virtual List<NodeAndReference> OtherReferencedNodes { get; set; } = new List<NodeAndReference>();

        public virtual List<NodeAndReference> OtherReferencingNodes { get; set; } = new List<NodeAndReference>();

        /// <summary>
        /// Indicates that Properties, DataVariables, OtherReferencedNodes etc. have not been populated. Use to support incremental rendering of the node model graph.
        /// </summary>
        public bool ReferencesNotResolved { get; set; }

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
        public string ModellingRule { get; set; }

        public virtual NodeModel Parent
        {
            get => _parent;
            set
            {
                if (_parent != null && _parent != value)
                {
                    // Changing parent or multiple parents on {this}: new parent {value}, previous parent {_parent}
                    return;
                }
                _parent = value;
            }
        }

        private NodeModel _parent;
    }

    public abstract class InstanceModel : InstanceModelBase
    {
        public string TypeDefinition { get; set; }
    }

    public class ObjectModel : InstanceModel
    {
        /// <summary>
        /// 0x0: The Object or View produces no event and has no event history.
        /// 0x1: The Object or View produces event notifications.
        /// 0x4: The Object has an event history which may be read.
        /// 0x8: The Object has an event history which may be updated.
        /// </summary>
        public byte? EventNotifier { get; set; }

        /// <summary>
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.Objects and ObjectModel for EF
        /// </summary>
        public virtual List<NodeModel> NodesWithObjects { get; set; } = new List<NodeModel>();
    }

    public abstract class BaseTypeModel : NodeModel
    {
        public bool IsAbstract { get; set; }

        public virtual string SuperType { get; set; }
    }

    public class ObjectTypeModel : BaseTypeModel
    {
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.Events and ObjectTypeModel for EF
        public virtual List<NodeModel> NodesWithEvents { get; set; } = new List<NodeModel>();
    }

    public class InterfaceModel : ObjectTypeModel
    {
        /// <summary>
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.Interfaces and InterfaceModel for EF
        /// </summary>
        public virtual List<NodeModel> NodesWithInterface { get; set; } = new List<NodeModel>();
    }

    public class VariableModel : InstanceModel, IVariableDataTypeInfo
    {
        public virtual string DataType { get; set; }
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
        /// <summary>
        /// Default value of the variable represented as a JSON-encoded Variant, i.e. {\"Value\":{\"Type\":10,\"Body\":0 }
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.Properties and PropertiesModel for EF
        /// </summary>
        public virtual List<NodeModel> NodesWithProperties { get; set; } = new List<NodeModel>();

        // Engineering units:
        public class EngineeringUnitInfo
        {
            /// <summary>
            /// If only DisplayName is specified, it is assumed to be the DisplayName or the Description of a UNECE unit as specified in https://reference.opcfoundation.org/v104/Core/docs/Part8/5.6.3/, and the referenced http://www.opcfoundation.org/UA/EngineeringUnits/UNECE/UNECE_to_OPCUA.csv
            /// </summary>
            public LocalizedText DisplayName { get; set; }
            public LocalizedText Description { get; set; }
            public string NamespaceUri { get; set; }
            public int? UnitId { get; set; }
        }

        // Engineering Units
        virtual public EngineeringUnitInfo EngineeringUnit { get; set; }

        /// <summary>
        /// NodeId to use for the engineering unit property. A random one can be generated by an exporter if not specified.
        /// </summary>
        public string EngUnitNodeId { get; set; }

        public string EngUnitModellingRule { get; set; }

        public uint? EngUnitAccessLevel { get; set; }

        // EU Range
        public double? MinValue { get; set; }

        public double? MaxValue { get; set; }

        /// <summary>
        /// NodeId to use for the EURange property. A random one can be generated by an exporter if not specified.
        /// </summary>
        public string EURangeNodeId { get; set; }

        public string EURangeModellingRule { get; set; }

        public uint? EURangeAccessLevel { get; set; }

        // Instrument Range
        public double? InstrumentMinValue { get; set; }

        public double? InstrumentMaxValue { get; set; }

        public string InstrumentRangeNodeId { get; set; }

        public string InstrumentRangeModellingRule { get; set; }

        public uint? InstrumentRangeAccessLevel { get; set; }

        public long? EnumValue { get; set; }

        public uint? AccessLevel { get; set; }

        public ushort? AccessRestrictions { get; set; }

        public uint? WriteMask { get; set; }

        public uint? UserWriteMask { get; set; }

        public double? MinimumSamplingInterval { get; set; }
    }

    public class DataVariableModel : VariableModel
    {
        /// <summary>
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.DataVariables and DataVariableModel for EF
        /// </summary>
        public virtual List<NodeModel> NodesWithDataVariables { get; set; } = new List<NodeModel>();
    }

    public class PropertyModel : VariableModel
    {
    }

    public class MethodModel : InstanceModel
    {
        /// <summary>
        /// InputArguments are a merged representation of the InputArguments property and any HasArgumentDescription references
        /// The NodeId will be NULL if there was no ArgumentDescription
        /// </summary>
        public List<VariableModel> InputArguments { get; set; }

        public List<VariableModel> OutputArguments { get; set; }

        /// <summary>
        /// Not used by the model itself. Captures the many-to-many relationship between NodeModel.Methods and MethodModel for EF
        /// </summary>
        public virtual List<NodeModel> NodesWithMethods { get; set; } = new List<NodeModel>();
    }

    public class ReferenceTypeModel : BaseTypeModel
    {
        /// <summary>
        /// The inverse name for the reference.
        /// </summary>
        public List<LocalizedText> InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric.
        /// </summary>
        public bool Symmetric { get; set; }
    }


    public interface IVariableDataTypeInfo
    {
        string DataType { get; set; }

        /// <summary>
        /// n > 1: the Value is an array with the specified number of dimensions.
        /// OneDimension(1) : The value is an array with one dimension.
        /// OneOrMoreDimensions(0): The value is an array with one or more dimensions.
        /// Scalar(−1): The value is not an array.
        /// Any(−2): The value can be a scalar or an array with any number of dimensions.
        /// ScalarOrOneDimension(−3): The value can be a scalar or a one dimensional array.
        /// </summary>
        int? ValueRank { get; set; }
        /// <summary>
        /// Comma separated list
        /// </summary>
        string ArrayDimensions { get; set; }
        string Value { get; set; }
    }

    public class VariableTypeModel : BaseTypeModel, IVariableDataTypeInfo
    {
        public virtual string DataType { get; set; }

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
    }

    public class DataTypeModel : BaseTypeModel
    {
        public virtual List<StructureField> StructureFields { get; set; }

        public virtual List<UaEnumField> EnumFields { get; set; }

        public bool? IsOptionSet { get; set; }

        public class StructureField
        {
            public string Name { get; set; }

            public string SymbolicName { get; set; }

            public virtual string DataType { get; set; }

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

            public uint? MaxStringLength { get; set; }

            public virtual List<LocalizedText> Description { get; set; }

            public bool IsOptional { get; set; }

            public bool AllowSubTypes { get; set; }

            /// <summary>
            /// Used to preserve field order if stored in a relational database (via EF etc.)
            /// </summary>
            public int FieldOrder { get; set; }

            public StructureField() { }
            public StructureField(StructureField field)
            {
                this.Name = field.Name;
                this.SymbolicName = field.SymbolicName;
                this.DataType = field.DataType;
                this.ValueRank = field.ValueRank;
                this.ArrayDimensions = field.ArrayDimensions;
                this.MaxStringLength = field.MaxStringLength;
                this.Description = field.Description;
                this.IsOptional = field.IsOptional;
                this.AllowSubTypes = field.AllowSubTypes;
                this.FieldOrder = field.FieldOrder;
            }

            public override string ToString() => $"{Name}: {DataType} {(IsOptional ? "Optional" : "")}";
        }

        public class StructureFieldWithOwner : StructureField
        {
            public StructureFieldWithOwner(StructureField field, DataTypeModel owner) : base(field)
            {
                Owner = owner;
            }
            public DataTypeModel Owner { get; set; }
        }

        public class UaEnumField
        {
            public string Name { get; set; }

            public string SymbolicName { get; set; }

            public virtual List<LocalizedText> DisplayName { get; set; }

            public virtual List<LocalizedText> Description { get; set; }

            public long Value { get; set; }

            public override string ToString() => $"{Name} = {Value}";
        }
    }
}
