
namespace AdminShell
{
    using JsonSubTypes;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [JsonConverter(typeof(JsonSubtypes), "ModelType")]
    public class SubmodelElement : Referable
    {
        public static Type[] PROP_MLP = new Type[]
        {
            typeof(MultiLanguageProperty), typeof(Property)
        };

        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public SemanticId SemanticId { get; set; } = new();

        [DataMember(Name = "qualifiers")]
        [XmlArray(ElementName = "qualifiers")]
        [XmlArrayItem(ElementName = "qualifier")]
        public List<Qualifier> Qualifiers { get; set; } = new();

        [DataMember(Name = "kind")]
        [XmlElement(ElementName = "kind")]
        public ModelingKind Kind { get; set; } = new();

        public SubmodelElement()
            : base() { }

        public SubmodelElement(SubmodelElement src)
            : base(src)
        {
            if (src == null)
            {
                return;
            }

            SemanticId = src.SemanticId;

            Kind = src.Kind;

            foreach (var q in src.Qualifiers)
            {
                Qualifiers.Add(new Qualifier(q));
            }
        }
    }
}

