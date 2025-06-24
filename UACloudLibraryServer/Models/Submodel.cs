
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    [DataContract]
    public class Submodel : Identifiable
    {
        [DataMember(Name = "embeddedDataSpecifications")]
        [XmlArray(ElementName = "embeddedDataSpecifications")]
        public List<EmbeddedDataSpecification> EmbeddedDataSpecifications { get; set; } = new();

        [DataMember(Name = "qualifiers")]
        [XmlArray(ElementName = "qualifiers")]
        public List<Qualifier> Qualifiers { get; set; } = new();

        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public Reference SemanticId { get; set; } = new();

        [DataMember(Name = "kind")]
        [XmlElement(ElementName = "kind")]
        public ModelingKind Kind { get; set; } = new();

        [DataMember(Name = "submodelElements")]
        [XmlArray(ElementName = "submodelElements")]
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
        public List<SubmodelElement> SubmodelElements { get; set; } = new();

        public Submodel()
            : base()
        {
        }

        public Submodel(Submodel other)
            : base()
        {
            if (other == null)
            {
                return;
            }

            foreach (var ed in other.EmbeddedDataSpecifications)
            {
                EmbeddedDataSpecifications.Add(new EmbeddedDataSpecification(ed));
            }

            foreach (var q in other.Qualifiers)
            {
                Qualifiers.Add(new Qualifier(q));
            }

            SemanticId = new Reference(other.SemanticId);

            Kind = other.Kind;

            foreach (var sme in other.SubmodelElements)
            {
                SubmodelElements.Add(new SubmodelElement(sme));
            }
        }
    }
}
