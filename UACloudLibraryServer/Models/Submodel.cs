
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    [DataContract]
    public class Submodel
    {
        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public string SemanticId { get; set; }

        [DataMember(Name = "submodelElements")]
        [XmlArray(ElementName = "submodelElements")]
        [XmlArrayItem(ElementName = "property", Type = typeof(AASProperty))]
        [XmlArrayItem(ElementName = "multiLanguageProperty", Type = typeof(MultiLanguageProperty))]
        [XmlArrayItem(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
        [XmlArrayItem(ElementName = "submodelElementList", Type = typeof(SubmodelElementList))]
        [XmlArrayItem(ElementName = "submodelElementStruct", Type = typeof(SubmodelElementStruct))]
        [XmlArrayItem(ElementName = "file", Type = typeof(File))]
        public List<SubmodelElement> SubmodelElements { get; set; } = new();
    }
}
