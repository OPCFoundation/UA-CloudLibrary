
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class UnitId
    {
        [DataMember(Name = "keys")]
        [XmlArray(ElementName = "keys")]
        [XmlArrayItem(ElementName = "key")]
        public List<Key> Keys { get; set; } = new();

        public UnitId() { }

        public UnitId(UnitId src)
        {
            if (src.Keys != null)
                foreach (var k in src.Keys)
                    Keys.Add(new Key(k));
        }
    }
}
