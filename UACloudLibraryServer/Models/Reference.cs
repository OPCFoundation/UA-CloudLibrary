
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [XmlType(TypeName="reference")]
    public class Reference
    {
        [XmlElement(ElementName="type")]
        [DataMember(Name="type")]
        public KeyElements Type { get; set; } = KeyElements.GlobalReference;

        [DataMember(Name="keys")]
        [XmlArray(ElementName="keys")]
        [XmlArrayItem(ElementName="key")]
        public List<Key> Keys { get; set; } = new();

        [XmlIgnore]
        public int Count { get { return Keys.Count; } }

        public Reference(){ }

        public Reference(Reference src)
        {
            if (src != null)
                foreach (var k in src.Keys)
                    Keys.Add(new Key(k));
        }

        public Key GetAsExactlyOneKey()
        {
            if (Keys == null || Keys.Count != 1)
                return null;

            var k = Keys[0];

            return new Key(k.Type.ToString(), k.Value);
        }

        public bool Matches(Identifier other)
        {
            if (other == null)
                return false;

            if (Count == 1)
            {
                var k = Keys[0];
                return k.Matches(other.IdType, other.Id, other.Value, MatchMode.Identification);
            }
            return false;
        }

        public bool Matches(Reference other, MatchMode matchMode = MatchMode.Strict)
        {
            if (Keys == null || other == null || other.Keys == null || other.Count != Count)
                return false;

            var same = true;

            for (int i = 0; i < Count; i++)
            {
                same = same && Keys[i].Matches(other.Keys[i], matchMode);
            }

            return same;
        }
    }
}
