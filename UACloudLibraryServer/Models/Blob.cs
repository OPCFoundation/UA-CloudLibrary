
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class Blob : DataElement
    {
        [Required]
        [DataMember(Name = "contentType")]
        [XmlElement(ElementName = "mimeType")]
        [MetaModelNameAttribute("Blob.MimeType")]
        public string MimeType { get; set; } = string.Empty;

        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        [MetaModelNameAttribute("Blob.Value")]
        public string Value { get; set; } = string.Empty;

        public Blob()
        {
            ModelType = ModelTypes.Blob;
        }

        public Blob(SubmodelElement src)
            : base(src)
        {
            if (!(src is Blob blb))
            {
                return;
            }

            MimeType = blb.MimeType;
            Value = blb.Value;
            ModelType = ModelTypes.Blob;
        }
    }
}
