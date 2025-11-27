
namespace AdminShell
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [XmlType(TypeName = "reference")]
    public class Reference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        [XmlElement(ElementName = "type")]
        [DataMember(Name = "type")]
        public virtual KeyElements Type { get; set; } = KeyElements.GlobalReference;

        [DataMember(Name = "keys")]
        [XmlArray(ElementName = "keys")]
        [XmlArrayItem(ElementName = "key")]
        public virtual List<Key> Keys { get; set; } = new();

        [XmlIgnore]
        public virtual int Count { get { return Keys.Count; } }

        public Reference() { }

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
