
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class AASProperty : DataElement
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        [MetaModelNameAttribute("AASProperty.Value")]
        public string Value { get; set; }

        [DataMember(Name = "valueId")]
        [XmlElement(ElementName = "valueId")]
        public GlobalReference ValueId { get; set; }

        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        [MetaModelNameAttribute("AASProperty.ValueType")]
        public string ValueType { get; set; }

        public AASProperty()
        {
            ModelType = ModelTypes.Property;
        }

        public AASProperty(SubmodelElement src)
            : base(src)
        {
            if (!(src is AASProperty p))
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

