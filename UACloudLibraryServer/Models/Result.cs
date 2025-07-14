
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Result
    {
        [DataMember(Name="messages")]
        [XmlArray(ElementName = "messages")]
        public List<Message> Messages { get; set; }

        [DataMember(Name="success")]
        [XmlElement(ElementName = "success")]
        public bool? Success { get; set; }
    }
}
