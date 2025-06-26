
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    // Note: In versions prior to V2.0.1, the SDK has "HasDataSpecification" containing only a Reference.
    // In 2.0.1, theoretically each entity with HasDataSpecification could also contain an EmbeddedDataSpecification.

    [DataContract]
    public class HasDataSpecification : List<EmbeddedDataSpecification>
    {
        [DataMember(Name = "reference")]
        [XmlArray(ElementName = "reference")]
        public List<Reference> Reference { get; set; } = new();

        public HasDataSpecification() { }

        public HasDataSpecification(HasDataSpecification src)
        {
            foreach (var r in src.Reference)
            {
                Reference.Add(r);
            }
        }
    }
}
