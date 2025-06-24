
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class EmbeddedDataSpecification
    {
        [Required]
        [DataMember(Name = "dataSpecification")]
        [XmlElement(ElementName = "dataSpecification")]
        public Reference DataSpecification { get; set; }

        [Required]
        [DataMember(Name = "dataSpecificationContent")]
        [XmlElement(ElementName = "dataSpecificationContent")]
        public DataSpecificationContent DataSpecificationContent { get; set; }

        public EmbeddedDataSpecification() { }

        public EmbeddedDataSpecification(EmbeddedDataSpecification src)
        {
            if (src.DataSpecification != null)
                DataSpecification = new Reference(src.DataSpecification);

            if (src.DataSpecificationContent != null)
                DataSpecificationContent = new DataSpecificationContent(src.DataSpecificationContent);
        }
    }
}
