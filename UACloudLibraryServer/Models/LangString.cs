
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
        [MetaModelName("LangString.Language")]
        public string Language { get; set; } = "en";

        [Required]
        [DataMember(Name = "text")]
        [XmlText]
        [MetaModelName("LangString.Text")]
        public string Text { get; set; } = string.Empty;

        public LangString() { }

        public LangString(LangString src)
        {
            Language = src.Language;
            Text = src.Text;
        }
    }
}

