
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class LangStringIEC61360
    {
        [DataMember(Name = "langString")]
        [XmlArray(ElementName = "langString")]
        public List<LangString> LangString = new List<LangString>();
    }
}
