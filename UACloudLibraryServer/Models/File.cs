
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class File : SubmodelElement
    {
        [Required]
        [DataMember(Name = "contentType")]
        [XmlElement(ElementName = "mimeType")]
        public string MimeType { get; set; }

        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }

        public File(SubmodelElement src)
        {
            if (!(src is File file))
            {
                return;
            }

            MimeType = file.MimeType;
            Value = file.Value;
        }
    }
}

