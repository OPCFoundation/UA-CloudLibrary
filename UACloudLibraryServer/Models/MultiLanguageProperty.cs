
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class MultiLanguageProperty : DataElement
    {
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public List<LangString> Value { get; set; } = new();

        [DataMember(Name = "valueId")]
        [XmlElement(ElementName = "valueId")]
        public GlobalReference ValueId { get; set; }

        public MultiLanguageProperty()
        {
            ModelType = ModelTypes.MultiLanguageProperty;
        }

        public MultiLanguageProperty(SubmodelElement src)
            : base(src)
        {
            if (!(src is MultiLanguageProperty mlp))
            {
                return;
            }

            Value = new List<LangString>(mlp.Value);
            ModelType = ModelTypes.MultiLanguageProperty;

            if (mlp.ValueId != null)
            {
                ValueId = new GlobalReference(mlp.ValueId);
            }
        }
    }
}
