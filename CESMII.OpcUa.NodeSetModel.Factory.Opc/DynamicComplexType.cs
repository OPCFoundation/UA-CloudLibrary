/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Export.Opc;

namespace Opc.Ua.Client.ComplexTypes
{
    /// <summary>
    /// A complex type that performs encoding and decoding based on NodeSetModel type information, without requiring a concrete CLR type
    /// </summary>
    public class DynamicComplexType :
        IEncodeable, IJsonEncodeable, IFormattable, IComplexTypeInstance, IDynamicComplexTypeInstance
    {
        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        public DynamicComplexType()
        {

        }

        #endregion Constructors

        #region Public Properties
        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId { get; set; }

        /// <summary>
        /// Makes a deep copy of the object.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public new virtual object MemberwiseClone()
        {
            throw new NotImplementedException();
            //Type thisType = this.GetType();
            //BaseComplexType clone = Activator.CreateInstance(thisType) as BaseComplexType;

            //clone.TypeId = TypeId;
            //clone.BinaryEncodingId = BinaryEncodingId;
            //clone.XmlEncodingId = XmlEncodingId;

            //// clone all properties of derived class
            //foreach (var property in GetPropertyEnumerator())
            //{
            //    property.SetValue(clone, Utils.Clone(property.GetValue(this)));
            //}

            //return clone;
        }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder)
        {
            InitializeDynamicEncodeable(encoder.Context);

            encoder.PushNamespace(XmlNamespace);
            foreach (DynamicTypePropertyInfo property in m_propertyList)
            {
                EncodeProperty(encoder, property);
                if (m_IsUnion && m_propertyDict.TryGetValue("SwitchField", out var sfObject) && sfObject is UInt32 sf && sf == 0)
                {
                    // No fields
                    break;
                }
            }
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder)
        {
            InitializeDynamicEncodeable(decoder.Context);
            decoder.PushNamespace(XmlNamespace);
            UInt32? encodingMask = null;
            UInt32 currentBit = 0;

            foreach (DynamicTypePropertyInfo property in m_propertyList)
            {
                if (encodingMask == null)
                {
                    DecodeProperty(decoder, property);
                    if (currentBit == 0 && property.Name == "EncodingMask")
                    {
                        // read encoding mask, but only if it's the first property (currentBit != 0 on subsequent iterations)
                        encodingMask = (UInt32)m_propertyDict["EncodingMask"];
                    }
                    currentBit = 0x01;
                }
                else
                {
                    if (!property.IsOptional || (currentBit & encodingMask ?? UInt32.MaxValue) != 0)
                    {
                        // Only decode non-optional properties or optional properties that are in encoding mask
                        DecodeProperty(decoder, property);
                    }
                    if (property.IsOptional)
                    {
                        currentBit <<= 1;
                    }
                }
                if (m_IsUnion && m_propertyDict.TryGetValue("SwitchField", out var sfObject) && sfObject is UInt32 sf && sf == 0)
                {
                    // No fields
                    break;
                }
            }

            decoder.PopNamespace();
        }

        private void InitializeDynamicEncodeable(IServiceMessageContext context)
        {
            if (m_propertyDict == null && this.TypeId != null && context.Factory is IDynamicEncodeableFactory dynamicFactory)
            {
                var dataType = dynamicFactory.GetDataTypeForEncoding(this.TypeId);
                if (dataType == null)
                {
                    dataType = dynamicFactory.GetDataTypeForEncoding(this.XmlEncodingId);
                }
                if (dataType == null)
                {
                    dataType = dynamicFactory.GetDataTypeForEncoding(this.BinaryEncodingId);
                }
                if (dataType == null)
                {
                    dataType = dynamicFactory.GetDataTypeForEncoding(this.JsonEncodingId);
                }
                if (dataType != null)
                {
                    var dtExpandedNodeId = ExpandedNodeId.Parse(dataType.NodeId);
                    var dtNodeId = ExpandedNodeId.ToNodeId(dtExpandedNodeId, context.NamespaceUris);
                    var builtInType = TypeInfo.GetBuiltInType(dtNodeId);
                    if (builtInType != BuiltInType.Null && builtInType != BuiltInType.ExtensionObject)
                    {
                        return;
                    }

                    if (XmlNamespace == null)
                    {
                        XmlNamespace = GetXmlNamespace(dataType.NodeSet);
                    }
                    if (_xmlName == null)
                    {
                        var typeName = dataType.SymbolicName ?? dataType.DisplayName?.FirstOrDefault()?.Text;
                        _xmlName = new XmlQualifiedName(typeName, XmlNamespace);
                    }

                    if (BinaryEncodingId == null)
                    {
                        var binaryEncodingId = dataType.OtherReferencedNodes.FirstOrDefault(rn =>
                            rn.ReferenceType?.NodeId == new ExpandedNodeId(ReferenceTypeIds.HasEncoding, Namespaces.OpcUa).ToString()
                            && rn.Node.BrowseName == $"{Namespaces.OpcUa};{BrowseNames.DefaultBinary}"
                        )?.Node.NodeId;
                        if (binaryEncodingId != null)
                        {
                            BinaryEncodingId = ExpandedNodeId.Parse(binaryEncodingId, context.NamespaceUris);
                        }
                        else
                        {
                            BinaryEncodingId = this.TypeId;
                        }
                    }
                    if (XmlEncodingId == null)
                    {
                        var xmlEncodingId = dataType.OtherReferencedNodes.FirstOrDefault(rn =>
                            rn.ReferenceType?.NodeId == new ExpandedNodeId(ReferenceTypeIds.HasEncoding, Namespaces.OpcUa).ToString()
                            && rn.Node.BrowseName == $"{Namespaces.OpcUa};{BrowseNames.DefaultXml}"
                        )?.Node.NodeId;
                        if (xmlEncodingId != null)
                        {
                            XmlEncodingId = ExpandedNodeId.Parse(xmlEncodingId, context.NamespaceUris);
                        }
                        else
                        {
                            XmlEncodingId = this.TypeId;
                        }
                    }
                    if (JsonEncodingId == null)
                    {
                        var jsonEncodingId = dataType.OtherReferencedNodes.FirstOrDefault(rn =>
                            rn.ReferenceType?.NodeId == new ExpandedNodeId(ReferenceTypeIds.HasEncoding, Namespaces.OpcUa).ToString()
                            && rn.Node.BrowseName == $"{Namespaces.OpcUa};{BrowseNames.DefaultJson}"
                        )?.Node.NodeId;
                        if (jsonEncodingId != null)
                        {
                            JsonEncodingId = ExpandedNodeId.Parse(jsonEncodingId, context.NamespaceUris);
                        }
                        else
                        {
                            JsonEncodingId = this.TypeId;
                        }
                    }

                    var propertyTypeInfo = GetEncodeableTypeInfo(dataType, dynamicFactory, context.NamespaceUris);

                    m_propertyList = propertyTypeInfo.ToList();
                    m_propertyDict = m_propertyList.ToDictionary(p => p.Name, p => (object)null);
                }
            }
        }

        private static string GetXmlNamespace(NodeSetModel nodeSet)
        {
            return nodeSet.XmlSchemaUri ?? $"{nodeSet.ModelUri.TrimEnd('/')}/Types.xsd";
        }

        public List<DynamicTypePropertyInfo> GetEncodeableTypeInfo(DataTypeModel dataType, IDynamicEncodeableFactory dynamicFactory, NamespaceTable namespaceUris)
        {
            List<DynamicTypePropertyInfo> properties = new();
            var fields = dataType.GetStructureFieldsInherited();
            if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Union}"))
            {
                m_IsUnion = true;
                if (!fields.Any(f => f.Name == "StructureField"))
                {
                    // Union: add switchfield
                    var property = new DynamicTypePropertyInfo
                    {
                        Name = "SwitchField",
                        TypeId = DataTypeIds.UInt32,
                        ValueRank = -1,
                        BuiltInType = BuiltInType.UInt32,
                        IsOptional = false,
                    };
                    properties.Add(property);
                }
            }
            if (fields.Any() /* || dataType.HasBaseType(new ExpandedNodeId(DataTypeIds.Structure, Namespaces.OpcUa).ToString())*/)
            {
                bool hasOptionalFields = false;
                foreach (var field in fields)
                {
                    var fieldDt = field.DataType as DataTypeModel;
                    if (field.IsOptional)
                    {
                        hasOptionalFields = true;
                    }
                    DynamicTypePropertyInfo property = GetPropertyTypeInfo(field.SymbolicName ?? field.Name, fieldDt, field.IsOptional, field.AllowSubTypes, field.ValueRank, dynamicFactory, namespaceUris);
                    property.XmlSchemaUri = field.Owner.NodeSet.XmlSchemaUri;
                    properties.Add(property);
                }
                if (hasOptionalFields)
                {
                    properties.Insert(0, new DynamicTypePropertyInfo { BuiltInType = BuiltInType.UInt32, IsOptional = true, Name = "EncodingMask", ValueRank = -1 });
                }
            }
            else if (dataType.EnumFields?.Any() == true || dataType.HasBaseType(new ExpandedNodeId(DataTypeIds.Enumeration, Namespaces.OpcUa).ToString()))
            {
                DynamicTypePropertyInfo property = GetPropertyTypeInfo(dataType.BrowseName, dataType, false, false, null, dynamicFactory, namespaceUris);
                property.IsEnum = true;
                properties.Add(property);
            }
            return properties;
        }

        private static DynamicTypePropertyInfo GetPropertyTypeInfo(string propertyName, DataTypeModel dataType, bool isOptional, bool allowSubTypes, int? valueRank, IDynamicEncodeableFactory dynamicFactory, NamespaceTable namespaceUris)
        {
            var builtInType = DynamicEncodeableFactory.GetBuiltInType(dataType as DataTypeModel, namespaceUris);
            Type systemType = builtInType != BuiltInType.Null ? null : typeof(DynamicComplexType);
            bool isEnum = false;
            if (builtInType == BuiltInType.Null && dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Enumeration}"))
            {
                isEnum = true;
                // Get the current application domain for the current thread.
                AppDomain currentDomain = AppDomain.CurrentDomain;

                // Create a dynamic assembly in the current application domain,
                // and allow it to be executed and saved to disk.
                var aName = new AssemblyName("TempAssembly");
                var ab = AssemblyBuilder.DefineDynamicAssembly(
                    aName, AssemblyBuilderAccess.Run);

                // Define a dynamic module in "TempAssembly" assembly. For a single-
                // module assembly, the module has the same name as the assembly.
                ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);

                // Define a public enumeration with the name "Elevation" and an
                // underlying type of Integer.
                EnumBuilder eb = mb.DefineEnum(dataType.SymbolicName ?? dataType.DisplayName.FirstOrDefault().Text, TypeAttributes.Public, typeof(int));
                foreach (var enumField in dataType.EnumFields)
                {
                    eb.DefineLiteral(enumField.Name, (int)enumField.Value);
                }
                // Create the type and save the assembly.
                systemType = eb.CreateTypeInfo().AsType();
            }
            var encodings = dynamicFactory.AddEncodingsForDataType(dataType, namespaceUris);
            ExpandedNodeId binaryEncodingId = null, xmlEncodingId = null, jsonEncodingId = null;
            if (encodings != null)
            {
                encodings.TryGetValue(BrowseNames.DefaultBinary, out binaryEncodingId);
                encodings.TryGetValue(BrowseNames.DefaultXml, out xmlEncodingId);
                encodings.TryGetValue(BrowseNames.DefaultJson, out jsonEncodingId);
            }
            var property = new DynamicTypePropertyInfo
            {
                Name = propertyName,
                TypeId = DynamicEncodeableFactory.NormalizeNodeIdForEncodableFactory(ExpandedNodeId.Parse(dataType.NodeId), namespaceUris),
                BinaryEncodingId = binaryEncodingId,
                XmlEncodingId = xmlEncodingId,
                JsonEncodingId = jsonEncodingId,
                ValueRank = valueRank ?? -1,
                BuiltInType = builtInType,
                SystemType = systemType,
                IsOptional = isOptional,
                AllowSubTypes = allowSubTypes,
                IsEnum = isEnum,
            };
            return property;
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            if (!(encodeable is DynamicComplexType valueBaseType))
            {
                return false;
            }

            var valueType = valueBaseType.GetType();
            if (this.GetType() != valueType)
            {
                return false;
            }
            throw new NotImplementedException();
            //foreach (var property in GetPropertyEnumerator())
            //{
            //    if (!Utils.IsEqual(property.GetValue(this), property.GetValue(valueBaseType)))
            //    {
            //        return false;
            //    }
            //}

            //return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null, null);
        }
        #endregion Public Properties

        #region IFormattable Members
        /// <summary>
        /// Returns the string representation of the complex type.
        /// </summary>
        /// <param name="format">(Unused). Leave this as null</param>
        /// <param name="formatProvider">The provider of a mechanism for retrieving an object to control formatting.</param>
        /// <returns>
        /// A <see cref="System.String"/> containing the value of the current embeded instance in the specified format.
        /// </returns>
        /// <exception cref="FormatException">Thrown if the <i>format</i> parameter is not null</exception>
        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
            {
                StringBuilder body = new StringBuilder();

                foreach (var property in m_propertyList)
                {
                    AppendPropertyValue(formatProvider, body, m_propertyDict[property.Name], property.ValueRank);
                }

                if (body.Length > 0)
                {
                    return body.Append('}').ToString();
                }

                if (!NodeId.IsNull(this.TypeId))
                {
                    return String.Format(formatProvider, "{{{0}}}", this.TypeId);
                }

                return "(null)";
            }

            throw new FormatException(Utils.Format("Invalid format string: '{0}'.", format));
        }
        #endregion IFormattable Members

        #region IComplexTypeProperties
        /// <inheritdoc/>
        public virtual int GetPropertyCount()
        {
            return m_propertyList?.Count ?? 0;
        }

        /// <inheritdoc/>
        /// <inheritdoc/>
        public virtual object this[int index]
        {
            get => m_propertyDict[m_propertyList.ElementAt(index).Name];
            set => m_propertyDict[m_propertyList.ElementAt(index).Name] = value;
        }

        /// <inheritdoc/>
        public virtual object this[string name]
        {
            get => m_propertyDict[name];
            set => m_propertyDict[name] = value;
        }

        #endregion IComplexTypeProperties

        #region Private Members
        /// <summary>
        /// Formatting helper.
        /// </summary>
        private void AddSeparator(StringBuilder body)
        {
            if (body.Length == 0)
            {
                body.Append('{');
            }
            else
            {
                body.Append('|');
            }
        }

        /// <summary>
        /// Append a property to the value string.
        /// Handle arrays and enumerations.
        /// </summary>
        protected void AppendPropertyValue(
            IFormatProvider formatProvider,
            StringBuilder body,
            object value,
            int valueRank)
        {
            AddSeparator(body);
            if (valueRank >= 0 && value is Array array)
            {
                var rank = array.Rank;
                var dimensions = new int[rank];
                var mods = new int[rank];
                for (int ii = 0; ii < rank; ii++)
                {
                    dimensions[ii] = array.GetLength(ii);
                }

                for (int ii = rank - 1; ii >= 0; ii--)
                {
                    mods[ii] = dimensions[ii];
                    if (ii < rank - 1)
                    {
                        mods[ii] *= mods[ii + 1];
                    }
                }

                int count = 0;
                foreach (var item in array)
                {
                    bool needSeparator = true;
                    for (int dc = 0; dc < rank; dc++)
                    {
                        if ((count % mods[dc]) == 0)
                        {
                            body.Append('[');
                            needSeparator = false;
                        }
                    }
                    if (needSeparator)
                    {
                        body.Append(',');
                    }
                    AppendPropertyValue(formatProvider, body, item);
                    count++;
                    needSeparator = false;
                    for (int dc = 0; dc < rank; dc++)
                    {
                        if ((count % mods[dc]) == 0)
                        {
                            body.Append(']');
                            needSeparator = true;
                        }
                    }
                    if (needSeparator && count < array.Length)
                    {
                        body.Append(',');
                    }
                }
            }
            else if (valueRank >= 0 && value is IEnumerable enumerable)
            {
                bool first = true;
                body.Append('[');
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        body.Append(',');
                    }
                    AppendPropertyValue(formatProvider, body, item);
                    first = false;
                }
                body.Append(']');
            }
            else
            {
                AppendPropertyValue(formatProvider, body, value);
            }
        }

        /// <summary>
        /// Append a property to the value string.
        /// </summary>
        private void AppendPropertyValue(
            IFormatProvider formatProvider,
            StringBuilder body,
            object value)
        {
            if (value is byte[] x)
            {
                body.AppendFormat(formatProvider, "Byte[{0}]", x.Length);
                return;
            }

            if (value is XmlElement xmlElements)
            {
                body.AppendFormat(formatProvider, "<{0}>", xmlElements.Name);
                return;
            }

            body.AppendFormat(formatProvider, "{0}", value);
        }

        /// <summary>
        /// Encode a property based on the property type and value rank.
        /// </summary>
        protected void EncodeProperty(
            IEncoder encoder,
            DynamicTypePropertyInfo property
            )
        {
            int valueRank = property.ValueRank;
            BuiltInType builtInType = property.BuiltInType;
            var propertyValue = m_propertyDict[property.Name];
            if (propertyValue == null)
            {
                return;
            }
            if (property.XmlSchemaUri != null && property.XmlSchemaUri != XmlNamespace)
            {
                encoder.PushNamespace(property.XmlSchemaUri);
            }
            if (valueRank == ValueRanks.Scalar)
            {
                EncodeProperty(encoder, property.Name, propertyValue, builtInType, property.SystemType, false, property.AllowSubTypes);
            }
            else if (valueRank >= ValueRanks.OneDimension)
            {
                EncodePropertyArray(encoder, property.Name, propertyValue, builtInType, valueRank, false);
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                    "Cannot encode a property with unsupported ValueRank {0}.", valueRank);
            }
            if (property.XmlSchemaUri != null && property.XmlSchemaUri != XmlNamespace)
            {
                encoder.PopNamespace();
            }
        }

        /// <summary>
        /// Encode a scalar property based on the property type.
        /// </summary>
        private void EncodeProperty(IEncoder encoder, string name, object propertyValue, BuiltInType builtInType, Type systemType, bool isEnum, bool allowSubTypes)
        {
            if (systemType?.IsEnum == true)
            {
                isEnum = true;
                builtInType = BuiltInType.Enumeration;
            }
            switch (builtInType)
            {
                case BuiltInType.Boolean: encoder.WriteBoolean(name, (Boolean)propertyValue); break;
                case BuiltInType.SByte: encoder.WriteSByte(name, (SByte)propertyValue); break;
                case BuiltInType.Byte: encoder.WriteByte(name, (Byte)propertyValue); break;
                case BuiltInType.Int16: encoder.WriteInt16(name, (Int16)propertyValue); break;
                case BuiltInType.UInt16: encoder.WriteUInt16(name, (UInt16)propertyValue); break;
                case BuiltInType.Int32: encoder.WriteInt32(name, (Int32)propertyValue); break;
                case BuiltInType.UInt32: encoder.WriteUInt32(name, (UInt32)propertyValue); break;
                case BuiltInType.Int64: encoder.WriteInt64(name, (Int64)propertyValue); break;
                case BuiltInType.UInt64: encoder.WriteUInt64(name, (UInt64)propertyValue); break;
                case BuiltInType.Float: encoder.WriteFloat(name, (Single)propertyValue); break;
                case BuiltInType.Double: encoder.WriteDouble(name, (Double)propertyValue); break;
                case BuiltInType.String: encoder.WriteString(name, (String)propertyValue); break;
                case BuiltInType.DateTime: encoder.WriteDateTime(name, (DateTime)propertyValue); break;
                case BuiltInType.Guid: encoder.WriteGuid(name, (Uuid)propertyValue); break;
                case BuiltInType.ByteString: encoder.WriteByteString(name, (Byte[])propertyValue); break;
                case BuiltInType.XmlElement: encoder.WriteXmlElement(name, (XmlElement)propertyValue); break;
                case BuiltInType.NodeId: encoder.WriteNodeId(name, (NodeId)propertyValue); break;
                case BuiltInType.ExpandedNodeId: encoder.WriteExpandedNodeId(name, (ExpandedNodeId)propertyValue); break;
                case BuiltInType.StatusCode: encoder.WriteStatusCode(name, (StatusCode)propertyValue); break;
                case BuiltInType.DiagnosticInfo: encoder.WriteDiagnosticInfo(name, (DiagnosticInfo)propertyValue); break;
                case BuiltInType.QualifiedName: encoder.WriteQualifiedName(name, (QualifiedName)propertyValue); break;
                case BuiltInType.LocalizedText: encoder.WriteLocalizedText(name, (LocalizedText)propertyValue); break;
                case BuiltInType.DataValue: encoder.WriteDataValue(name, (DataValue)propertyValue); break;
                case BuiltInType.Variant: encoder.WriteVariant(name, (Variant)propertyValue); break;
                case BuiltInType.ExtensionObject: encoder.WriteExtensionObject(name, (ExtensionObject)propertyValue); break;
                case BuiltInType.Enumeration:
                    if (isEnum)
                    {
                        encoder.WriteEnumerated(name, propertyValue as Enum);
                        break;
                    }
                    goto case BuiltInType.Int32;
                default:
                    if (propertyValue is IEncodeable encodableValue)
                    {
                        if (allowSubTypes)
                        {
                            encoder.WriteExtensionObject(name, new ExtensionObject(encodableValue));
                        }
                        else
                        {
                            encoder.WriteEncodeable(name, encodableValue, systemType);
                        }
                        break;
                    }
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                        "Cannot encode unknown type {0}.", propertyValue?.GetType());
            }
        }

        /// <summary>
        /// Encode an array property based on the base property type.
        /// </summary>
        private void EncodePropertyArray(IEncoder encoder, string name, object propertyValue, BuiltInType builtInType, int valueRank, bool isEnum)
        {
            if (isEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            if (propertyValue != null)
            {
                encoder.WriteArray(name, propertyValue, valueRank, builtInType);
            }
        }

        /// <summary>
        /// Decode a property based on the property type and value rank.
        /// </summary>
        protected void DecodeProperty(
            IDecoder decoder,
            DynamicTypePropertyInfo property)
        {

            if (property.XmlSchemaUri != null && property.XmlSchemaUri != XmlNamespace)
            {
                decoder.PushNamespace(property.XmlSchemaUri);
            }

            int valueRank = property.ValueRank;
            if (valueRank == ValueRanks.Scalar)
            {
                DecodeProperty(decoder, property.Name, property.BuiltInType, property.SystemType, property.IsEnum, property.AllowSubTypes, property.TypeId);
            }
            else if (valueRank >= ValueRanks.OneDimension)
            {
                DecodePropertyArray(decoder, property.Name, property.BuiltInType, property.SystemType, valueRank, property.IsEnum, property.TypeId);
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Cannot decode a property with unsupported ValueRank {0}.", valueRank);
            }
            if (property.XmlSchemaUri != null && property.XmlSchemaUri != XmlNamespace)
            {
                decoder.PopNamespace();
            }
        }

        /// <summary>
        /// Decode a scalar property based on the property type.
        /// </summary>
        private void DecodeProperty(IDecoder decoder, string name, BuiltInType builtInType, Type systemType, bool isEnum, bool allowSubTypes, ExpandedNodeId typeId)
        {
            //var propertyType = property.PropertyType;
            if (systemType?.IsEnum == true)
            {
                isEnum = true;
                builtInType = BuiltInType.Enumeration;
            }
            switch (builtInType)
            {
                case BuiltInType.Boolean: m_propertyDict[name] = decoder.ReadBoolean(name); break;
                case BuiltInType.SByte: m_propertyDict[name] = decoder.ReadSByte(name); break;
                case BuiltInType.Byte: m_propertyDict[name] = decoder.ReadByte(name); break;
                case BuiltInType.Int16: m_propertyDict[name] = decoder.ReadInt16(name); break;
                case BuiltInType.UInt16: m_propertyDict[name] = decoder.ReadUInt16(name); break;
                case BuiltInType.Int32: m_propertyDict[name] = decoder.ReadInt32(name); break;
                case BuiltInType.UInt32: m_propertyDict[name] = decoder.ReadUInt32(name); break;
                case BuiltInType.Int64: m_propertyDict[name] = decoder.ReadInt64(name); break;
                case BuiltInType.UInt64: m_propertyDict[name] = decoder.ReadUInt64(name); break;
                case BuiltInType.Float: m_propertyDict[name] = decoder.ReadFloat(name); break;
                case BuiltInType.Double: m_propertyDict[name] = decoder.ReadDouble(name); break;
                case BuiltInType.String: m_propertyDict[name] = decoder.ReadString(name); break;
                case BuiltInType.DateTime: m_propertyDict[name] = decoder.ReadDateTime(name); break;
                case BuiltInType.Guid: m_propertyDict[name] = decoder.ReadGuid(name); break;
                case BuiltInType.ByteString: m_propertyDict[name] = decoder.ReadByteString(name); break;
                case BuiltInType.XmlElement: m_propertyDict[name] = decoder.ReadXmlElement(name); break;
                case BuiltInType.NodeId: m_propertyDict[name] = decoder.ReadNodeId(name); break;
                case BuiltInType.ExpandedNodeId: m_propertyDict[name] = decoder.ReadExpandedNodeId(name); break;
                case BuiltInType.StatusCode: m_propertyDict[name] = decoder.ReadStatusCode(name); break;
                case BuiltInType.QualifiedName: m_propertyDict[name] = decoder.ReadQualifiedName(name); break;
                case BuiltInType.LocalizedText: m_propertyDict[name] = decoder.ReadLocalizedText(name); break;
                case BuiltInType.DataValue: m_propertyDict[name] = decoder.ReadDataValue(name); break;
                case BuiltInType.Variant: m_propertyDict[name] = decoder.ReadVariant(name); break;
                case BuiltInType.DiagnosticInfo: m_propertyDict[name] = decoder.ReadDiagnosticInfo(name); break;
                case BuiltInType.ExtensionObject:
                    m_propertyDict[name] = decoder.ReadExtensionObject(name);
                    break;
                case BuiltInType.Enumeration:
                    if (isEnum)
                    {
                        m_propertyDict[name] = decoder.ReadEnumerated(name, systemType); break;
                    }
                    goto case BuiltInType.Int32;
                default:
                    Type encodeableType = null;
                    if (!decoder.Context.Factory.EncodeableTypes.TryGetValue(typeId, out encodeableType))
                    {
                        if (typeof(IEncodeable).IsAssignableFrom(systemType))
                        {
                            encodeableType = systemType;
                        }
                    }
                    if (encodeableType != null)
                    {
                        if (allowSubTypes)
                        {
                            m_propertyDict[name] = decoder.ReadExtensionObject(name)?.Body;
                        }
                        else
                        {
                            m_propertyDict[name] = decoder.ReadEncodeable(name, encodeableType, typeId);
                        }
                    }
                    else
                    {
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                            "Cannot decode unknown type {0} with encoding {1}.", systemType, typeId);
                        //m_propertyDict[name] = decoder.ReadEncodeable(name, typeof(DynamicComplexType), typeId);
                    }
                    break;
            }
        }

        /// <summary>
        /// Decode an array property based on the base property type.
        /// </summary>
        private void DecodePropertyArray(IDecoder decoder, string name, BuiltInType builtInType, Type systemType, int valueRank, bool isEnum, ExpandedNodeId typeId)
        {
            if (isEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            Array decodedArray = decoder.ReadArray(name, valueRank, builtInType/*, elementType*/, systemType, typeId);
            m_propertyDict[name] = decodedArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public XmlQualifiedName GetXmlName(IServiceMessageContext context)
        {
            InitializeDynamicEncodeable(context);
            return _xmlName;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        private XmlQualifiedName _xmlName;

        #endregion Private Members

        #region Protected Properties
        /// <summary>
        /// Provide XmlNamespace
        /// </summary>
        public string XmlNamespace { get; set; }

        #endregion

        #region Protected Fields
        /// <summary>
        /// The list of properties of this complex type.
        /// </summary>
        protected IList<DynamicTypePropertyInfo> m_propertyList;

        /// <summary>
        /// The list of properties as dictionary.
        /// </summary>
        protected Dictionary<string, object> m_propertyDict;
        private bool m_IsUnion;
        #endregion Protected Fields

        #region Private Fields
        //private XmlQualifiedName m_xmlName;
        #endregion Private Fields
    }

    /// <summary>
    /// 
    /// </summary>
    public class DynamicTypePropertyInfo
    {
        /// <inheritdoc/>
        public int ValueRank { get; set; }
        /// <inheritdoc/>
        public BuiltInType BuiltInType { get; set; }
        public Type SystemType { get; set; }
        /// <inheritdoc/>
        public string Name { get; set; }
        /// <summary>
        /// Indicates optional structure field: important for JSON and XML encodingmask
        /// </summary>
        public bool IsOptional { get; set; }
        /// <summary>
        /// Indicates if subtypes are allowed: uses ExtensionObject encoding to capture the type/encoding id
        /// </summary>
        public bool AllowSubTypes { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId { get; set; }
        /// <inheritdoc/>
        public ExpandedNodeId JsonEncodingId { get; set; }
        public bool IsEnum { get; set; }
        public string XmlSchemaUri { get; set; }

        public override string ToString() => $"{Name} {TypeId} {XmlEncodingId} {XmlSchemaUri}";
    }


}//namespace
