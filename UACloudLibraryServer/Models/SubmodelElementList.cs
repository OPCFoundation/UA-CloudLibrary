
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
        [XmlArrayItem(ElementName = "property", Type = typeof(Property))]
        [XmlArrayItem(ElementName = "multiLanguageProperty", Type = typeof(MultiLanguageProperty))]
        [XmlArrayItem(ElementName = "range", Type = typeof(Range))]
        [XmlArrayItem(ElementName = "file", Type = typeof(File))]
        [XmlArrayItem(ElementName = "blob", Type = typeof(Blob))]
        [XmlArrayItem(ElementName = "referenceElement", Type = typeof(ReferenceElement))]
        [XmlArrayItem(ElementName = "relationshipElement", Type = typeof(RelationshipElement))]
        [XmlArrayItem(ElementName = "annotatedRelationshipElement", Type = typeof(AnnotatedRelationshipElement))]
        [XmlArrayItem(ElementName = "capability", Type = typeof(Capability))]
        [XmlArrayItem(ElementName = "submodelElementCollection", Type = typeof(SubmodelElementCollection))]
        [XmlArrayItem(ElementName = "operation", Type = typeof(Operation))]
        [XmlArrayItem(ElementName = "basicEvent", Type = typeof(BasicEvent))]
        [XmlArrayItem(ElementName = "entity", Type = typeof(Entity))]
        [XmlArrayItem(ElementName = "submodelElementList", Type = typeof(SubmodelElementList))]
        [XmlArrayItem(ElementName = "submodelElementStruct", Type = typeof(SubmodelElementStruct))]
        [XmlArrayItem(ElementName = "globalReferenceElement", Type = typeof(GlobalReferenceElement))]
        [XmlArrayItem(ElementName = "modelReferenceElement", Type = typeof(ModelReferenceElement))]
        public List<SubmodelElement> Value { get; set; } = new();

        [DataMember(Name = "semanticIdValues")]
        [XmlElement(ElementName = "semanticIdValues")]
        public Reference SemanticIdValues { get; set; } = new();

        [DataMember(Name = "submodelElementTypeValues")]
        [XmlElement(ElementName = "submodelElementTypeValues")]
        public ModelTypes SubmodelElementTypeValues { get; set; } = new();

        [DataMember(Name = "valueTypeValues")]
        [XmlElement(ElementName = "valueTypeValues")]
        public string ValueTypeValues { get; set; }

        [DataMember(Name = "orderRelevant")]
        [XmlElement(ElementName = "orderRelevant")]
        public bool OrderRelevant = false;

        [DataMember(Name = "semanticIdListElement")]
        [XmlElement(ElementName = "semanticIdListElement")]
        public SemanticId SemanticIdListElement { get; set; } = new();

        [DataMember(Name = "typeValueListElement")]
        [XmlElement(ElementName = "typeValueListElement")]
        public string TypeValueListElement { get; set; }

        [DataMember(Name = "valueTypeListElement")]
        [XmlElement(ElementName = "valueTypeListElement")]
        public string ValueTypeListElement { get; set; }

        public SubmodelElementList()
        {
            ModelType = ModelTypes.SubmodelElementList;
        }

        public SubmodelElementList(SubmodelElementList src)
            : base(src)
        {
            if (!(src is SubmodelElementList sml))
            {
                return;
            }

            Value = sml.Value;
            OrderRelevant = sml.OrderRelevant;
            ModelType = ModelTypes.SubmodelElementList;

            if (sml.SemanticIdListElement != null)
            {
                SemanticIdListElement = new SemanticId(sml.SemanticIdListElement);
            }

            TypeValueListElement = sml.TypeValueListElement;
            ValueTypeListElement = sml.ValueTypeListElement;
        }
    }
}

