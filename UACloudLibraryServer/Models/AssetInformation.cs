
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class AssetInformation
    {
        [DataMember(Name = "globalAssetId")]
        [XmlElement(ElementName = "globalAssetId")]
        public string GlobalAssetId { get; set; }

        [DataMember(Name = "specificAssetIds")]
        [XmlArray(ElementName = "specificAssetIds")]
        public List<IdentifierKeyValuePair> SpecificAssetIds { get; set; }

        [DataMember(Name = "defaultThumbnail")]
        [XmlElement(ElementName = "defaultThumbnail")]
        public Resource DefaultThumbnail { get; set; }

        [DataMember(Name = "assetKind")]
        [XmlElement(ElementName = "assetKind")]
        public AssetKind AssetKind { get; set; } = new();
    }
}
