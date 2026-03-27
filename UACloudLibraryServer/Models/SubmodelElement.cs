
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    // Polymorphism is based on discriminator property "ModelType".
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "ModelType")]
    [JsonDerivedType(typeof(AASProperty), "AASProperty")]
    [JsonDerivedType(typeof(MultiLanguageProperty), "MultiLanguageProperty")]
    [JsonDerivedType(typeof(SubmodelElementCollection), "SubmodelElementCollection")]
    [JsonDerivedType(typeof(SubmodelElementList), "SubmodelElementList")]
    [JsonDerivedType(typeof(SubmodelElementStruct), "SubmodelElementStruct")]
    [JsonDerivedType(typeof(File), "file")]
    public class SubmodelElement
    {
        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public string SemanticId { get; set; }
    }
}

