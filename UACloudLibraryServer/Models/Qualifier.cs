
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Qualifier
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        [MetaModelName("Qualifier.Value")]
        public string Value { get; set; }

        [DataMember(Name = "valueId")]
        [XmlElement(ElementName = "valueId")]
        public GlobalReference ValueId { get; set; }

        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        [MetaModelName("Qualifier.ValueType")]
        public string ValueType { get; set; }

        [Required]
        [DataMember(Name = "type")]
        [XmlElement(ElementName = "type")]
        [MetaModelName("Qualifier.Type")]
        public string Type { get; set; }

        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public SemanticId SemanticId = null;

        public Qualifier() { }

        public Qualifier(Qualifier src)
        {
            if (src.SemanticId != null)
                SemanticId = new SemanticId(src.SemanticId);

            this.Type = src.Type;

            this.Value = src.Value;

            if (src.ValueId != null)
                this.ValueId = new GlobalReference(src.ValueId);
        }
    }
}
