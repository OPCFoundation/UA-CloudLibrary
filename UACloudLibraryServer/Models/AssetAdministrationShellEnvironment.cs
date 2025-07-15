
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [XmlRoot(ElementName = "environment")]
    public class AssetAdministrationShellEnvironment
    {
        [DataMember(Name = "assetAdministrationShells")]
        [XmlArray(ElementName = "assetAdministrationShells")]
        [XmlArrayItem(ElementName = "assetAdministrationShell")]
        public List<AssetAdministrationShell> AssetAdministrationShells { get; set; } = new();

        [DataMember(Name = "submodels")]
        [XmlArray(ElementName = "submodels")]
        [XmlArrayItem(ElementName = "submodel")]
        public List<Submodel> Submodels { get; set; } = new();

        [DataMember(Name = "conceptDescriptions")]
        [XmlArray(ElementName = "conceptDescriptions")]
        [XmlArrayItem(ElementName = "conceptDescription")]
        public List<ConceptDescription> ConceptDescriptions { get; set; } = new();
    }
}
