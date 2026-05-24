
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class AASProperty : SubmodelElement
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }

        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        public string ValueType { get; set; }
    }
}

