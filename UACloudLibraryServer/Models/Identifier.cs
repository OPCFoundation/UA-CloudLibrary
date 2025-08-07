
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    // V3.0 made Identification a simple string
    // In V1.0 and V2.0, it contained two attributes "IdType" and "Id"
    // As string is sealed, this class cannot derive dirctly from string,
    // so an implicit conversion is tested
    [DataContract]
    public class Identifier
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public string Value { get; set; } = string.Empty;

        [DataMember(Name = "idType")]
        [XmlAttribute(AttributeName = "idType")]
        public string IdType { get; set; } = string.Empty;

        [DataMember(Name = "id")]
        [XmlElement(ElementName = "id")]
        [XmlText]
        public string Id { get; set; } = string.Empty;

        public static implicit operator string(Identifier d)
        {
            return d.Value;
        }

        public static implicit operator Identifier(string d)
        {
            return new Identifier(d);
        }

        public Identifier() { }

        public Identifier(Identifier src)
        {
            Value = src.Value;
        }

        public Identifier(string id)
        {
            Value = id;
        }
    }
}

