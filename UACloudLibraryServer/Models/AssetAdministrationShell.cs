
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using JsonSubTypes;
    using Newtonsoft.Json;

    [DataContract]
    [JsonConverter(typeof(JsonSubtypes), "modelType.name")]
    public class AssetAdministrationShell : Identifiable
    {
        //[DataMember(Name = "embeddedDataSpecifications")]
        //[XmlArray(ElementName = "embeddedDataSpecifications")]
        //[XmlArrayItem(ElementName = "embeddedDataSpecification")]
        //public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; } = new();

        [DataMember(Name = "assetInformation")]
        [XmlElement(ElementName = "assetInformation")]
        public AssetInformation AssetInformation { get; set; } = new();

        [DataMember(Name = "submodels")]
        [XmlArray(ElementName = "submodelRefs")]
        [XmlArrayItem(ElementName = "submodelRef")]
        public List<ModelReference> Submodels { get; set; } = new();
    }
}
