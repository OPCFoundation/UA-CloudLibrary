
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class HasExtensions
    {
        [DataMember(Name="extensions")]
        [XmlArray(ElementName = "extensions")]
        public List<Extension> Extensions { get; set; }
    }
}
