
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Property : DataElement
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        [MetaModelName("Property.Value")]
        public string Value { get; set; }

        [DataMember(Name = "valueId")]
        [XmlElement(ElementName = "valueId")]
        public GlobalReference ValueId { get; set; }

        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        [MetaModelName("Property.ValueType")]
        public string ValueType { get; set; }

        public Property()
        {
            ModelType = ModelTypes.Property;
        }

        public Property(SubmodelElement src)
            : base(src)
        {
            if (!(src is Property p))
            {
                return;
            }

            ValueType = p.ValueType;
            ModelType = ModelTypes.Property;
            Value = p.Value;

            if (p.ValueId != null)
            {
                ValueId = new GlobalReference(p.ValueId);
            }
        }
    }
}

