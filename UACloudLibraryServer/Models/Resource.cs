
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AdminShell
{
    [DataContract]
    public class Resource
    {
        [DataMember(Name ="path")]
        [XmlElement(ElementName = "path")]
        [MetaModelName("Resource.Path")]
        public string Path = string.Empty;

        [DataMember(Name = "contentType")]
        [XmlElement(ElementName = "contentType")]
        [MetaModelName("Resource.ContentType")]
        public string ContentType = string.Empty;
    }
}
