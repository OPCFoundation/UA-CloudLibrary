
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class MultiLanguageProperty : SubmodelElement
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public List<LangString> Value { get; set; } = new();
    }
}
