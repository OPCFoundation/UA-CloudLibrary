
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Message
    {
        [DataMember(Name = "code")]
        [XmlElement(ElementName = "code")]
        public string Code { get; set; }

        [DataMember(Name = "messageType")]
        [XmlElement(ElementName = "messageType")]
        public MessageType? MessageType { get; set; }

        [DataMember(Name = "text")]
        [XmlElement(ElementName = "text")]
        public string Text { get; set; }

        [DataMember(Name = "timestamp")]
        [XmlElement(ElementName = "timestamp")]
        public string Timestamp { get; set; }
    }
}
