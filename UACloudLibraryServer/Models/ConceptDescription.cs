
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class ConceptDescription : Identifiable
    {
        [DataMember(Name = "embeddedDataSpecifications")]
        [XmlArray(ElementName = "embeddedDataSpecifications")]
        public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; }

        [DataMember(Name = "isCaseOf")]
        [XmlArray(ElementName = "isCaseOf")]
        public List<Reference> IsCaseOf { get; set; }
    }
}

