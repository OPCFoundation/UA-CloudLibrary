
namespace AdminShell
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class GlobalReference : Reference
    {
        [Required]
        [DataMember(Name = "value")]
        [XmlArray(ElementName = "values")]
        [XmlArrayItem(ElementName = "value")]
        public List<Identifier> Value { get; set; } = new();

        public GlobalReference() : base() { }

        public GlobalReference(GlobalReference src) : base()
        {
            if (src == null)
                return;

            foreach (var id in src.Value)
                Value.Add(new Identifier(id));
        }

        public GlobalReference(Reference r) : base() { }
    }
}
