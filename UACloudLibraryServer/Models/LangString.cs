
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    [XmlType(TypeName = "langString")]
    public class LangString
    {
        [Required]
        [DataMember(Name = "language")]
        [XmlAttribute(AttributeName = "lang")]
        public string Language { get; set; } = "en";

        [Required]
        [DataMember(Name = "text")]
        [XmlText]
        public string Text { get; set; } = string.Empty;
    }
}

