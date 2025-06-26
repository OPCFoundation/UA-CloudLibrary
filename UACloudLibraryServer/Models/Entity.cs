
namespace AdminShell
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class Entity : SubmodelElement
    {
        [Required]
        [DataMember(Name = "entityType")]
        [XmlElement(ElementName = "entityType")]
        public string EntityType { get; set; }

        [DataMember(Name = "globalAssetId")]
        [XmlElement(ElementName = "globalAssetId")]
        public string GlobalAssetId { get; set; }

        [DataMember(Name = "statements")]
        [XmlArray(ElementName = "statements")]
        public List<SubmodelElement> Statements { get; set; } = new();

        public Entity()
        {
            ModelType = ModelTypes.Entity;
        }

        public Entity(SubmodelElement src)
            : base(src)
        {
            if (!(src is Entity ent))
            {
                return;
            }

            if (ent.Statements != null)
            {
                Statements = new List<SubmodelElement>();
                foreach (var smw in ent.Statements)
                {
                    Statements.Add(smw);
                }
            }

            EntityType = ent.EntityType;
            ModelType = ModelTypes.Entity;
        }
    }
}
