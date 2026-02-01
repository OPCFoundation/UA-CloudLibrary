
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class SubmodelElementList : SubmodelElement
    {
        [DataMember(Name = "value")]
        [XmlArray(ElementName = "value")]
        [XmlArrayItem(ElementName = "property", Type = typeof(AASProperty))]
        [XmlArrayItem(ElementName = "multiLanguageProperty", Type = typeof(MultiLanguageProperty))]
        [XmlArrayItem(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
        [XmlArrayItem(ElementName = "submodelElementList", Type = typeof(SubmodelElementList))]
        [XmlArrayItem(ElementName = "globalReferenceElement", Type = typeof(GlobalReferenceElement))]
        public List<SubmodelElement> Value { get; set; } = new();

        [DataMember(Name = "valueTypeValues")]
        [XmlElement(ElementName = "valueTypeValues")]
        public string ValueTypeValues { get; set; }
    }
}

