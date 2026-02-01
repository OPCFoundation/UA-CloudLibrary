
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class GlobalReferenceElement : SubmodelElement
    {
        [DataMember(Name = "grvalue")]
        [XmlElement(ElementName = "grvalue")]
        public string GRValue { get; set; } = string.Empty;
    }
}

