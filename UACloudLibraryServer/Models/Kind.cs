
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    [DataContract]
    public class Kind
    {
        [DataMember(Name ="kind")]
        [XmlText]
        public string kind = "Instance";

        [XmlIgnore]
        public bool IsInstance { get { return kind == null || kind.Trim().ToLower() == "instance"; } }

        [XmlIgnore]
        public bool IsType { get { return kind != null && kind.Trim().ToLower() == "Type"; } }
    }
}
