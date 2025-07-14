
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class ModelReference : Reference
    {
        [DataMember(Name = "referredSemanticId")]
        [XmlElement(ElementName = "referredSemanticId")]
        public GlobalReference ReferredSemanticId { get; set; }

        public ModelReference()
        {
            Type = KeyElements.ModelReference;
        }

        public ModelReference(ModelReference src)
        {
            Type = KeyElements.ModelReference;

            if (src == null)
            {
                return;
            }

            if (src.ReferredSemanticId != null)
            {
                ReferredSemanticId = new GlobalReference(src.ReferredSemanticId);
            }

            foreach (var k in src.Keys)
            {
                Keys.Add(new Key(k));
            }
        }
    }
}
