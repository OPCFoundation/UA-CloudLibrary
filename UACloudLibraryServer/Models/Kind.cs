
namespace AdminShell
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    [DataContract]
    public class Kind
    {
        [DataMember(Name = "kind")]
        [XmlText]
        public string kind { get; set; } = "Instance";

        [XmlIgnore]
        public bool IsInstance { get { return kind == null || kind.Trim().Equals("instance", StringComparison.OrdinalIgnoreCase); } }

        [XmlIgnore]
        public bool IsType { get { return kind != null && kind.Trim().Equals("type", StringComparison.OrdinalIgnoreCase); } }
    }
}
