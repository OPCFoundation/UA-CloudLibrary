
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class Range : DataElement
    {
        [DataMember(Name = "max")]
        [XmlElement(ElementName = "max")]
        [MetaModelName("Range.Max")]
        public string Max { get; set; }

        [DataMember(Name = "min")]
        [XmlElement(ElementName = "min")]
        [MetaModelName("Range.Min")]
        public string Min { get; set; }

        [Required]
        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        [MetaModelName("Range.ValueType")]
        public string ValueType { get; set; }

        public Range()
        {
            ModelType = ModelTypes.Range;
        }

        public Range(SubmodelElement src)
            : base(src)
        {
            if (!(src is Range rng))
            {
                return;
            }

            ValueType = rng.ValueType;
            Min = rng.Min;
            Max = rng.Max;
            ModelType = ModelTypes.Range;
        }
    }
}
