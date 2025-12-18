
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class HasSemantics
    {
        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public virtual Reference SemanticId { get; set; }
    }
}
