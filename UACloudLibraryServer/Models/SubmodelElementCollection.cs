
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// In V2.0, this was the most important SME to hold multiple child SMEs.
    /// In V3.0, this is deprecated. Use SubmodelElementList, SubmodelElementStruct instead.
    /// </summary>
    [DataContract]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class SubmodelElementCollection : SubmodelElement
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        [DataMember(Name = "value")]
        [XmlArray(ElementName = "value")]
        [XmlArrayItem(ElementName = "property", Type = typeof(AASProperty))]
        [XmlArrayItem(ElementName = "multiLanguageProperty", Type = typeof(MultiLanguageProperty))]
        [XmlArrayItem(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
        [XmlArrayItem(ElementName = "submodelElementList", Type = typeof(SubmodelElementList))]
        [XmlArrayItem(ElementName = "globalReferenceElement", Type = typeof(GlobalReferenceElement))]
        public List<SubmodelElement> Value { get; set; } = new();
    }
}
