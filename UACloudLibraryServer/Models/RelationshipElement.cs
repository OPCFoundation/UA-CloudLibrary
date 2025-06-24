
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class RelationshipElement : DataElement
    {
        [Required]
        [DataMember(Name = "first")]
        [XmlElement(ElementName = "first")]
        public ModelReference First { get; set; } = new();

        [Required]
        [DataMember(Name = "second")]
        [XmlElement(ElementName = "second")]
        public ModelReference Second { get; set; } = new();

        public RelationshipElement()
        {
            ModelType = ModelTypes.RelationshipElement;
        }

        public RelationshipElement(SubmodelElement src)
            : base(src)
        {
            if (!(src is RelationshipElement rel))
            {
                return;
            }

            ModelType = ModelTypes.RelationshipElement;

            if (rel.First != null)
            {
                First = new ModelReference(rel.First);
            }

            if (rel.Second != null)
            {
                Second = new ModelReference(rel.Second);
            }
        }
    }
}
