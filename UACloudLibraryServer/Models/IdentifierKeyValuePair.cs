
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class IdentifierKeyValuePair : HasSemantics
    {
        [Required]
        [DataMember(Name = "key")]
        [XmlElement(ElementName = "key")]
        [MetaModelName("IdentifierKeyValuePair.Key")]
        public string Key { get; set; }

        [Required]
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        [MetaModelName("IdentifierKeyValuePair.Value")]
        public string Value { get; set; }

        [Required]
        [DataMember(Name = "subjectId")]
        [XmlElement(ElementName = "subjectId")]
        public Reference SubjectId { get; set; }

        [Required]
        [DataMember(Name = "externalSubjectId")]
        [XmlElement(ElementName = "externalSubjectId")]
        public GlobalReference ExternalSubjectId { get; set; }
    }
}
