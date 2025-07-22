
namespace AdminShell
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class BasicEvent : SubmodelElement
    {
        [Required]
        [DataMember(Name = "observed")]
        [XmlElement(ElementName = "observed")]
        public Reference Observed { get; set; } = new();

        public BasicEvent()
        {
            ModelType = ModelTypes.BasicEvent;
        }

        public BasicEvent(SubmodelElement src)
            : base(src)
        {
            if (!(src is BasicEvent be))
            {
                return;
            }

            ModelType = ModelTypes.BasicEvent;

            if (be.Observed != null)
            {
                Observed = new Reference(be.Observed);
            }
        }
    }
}
