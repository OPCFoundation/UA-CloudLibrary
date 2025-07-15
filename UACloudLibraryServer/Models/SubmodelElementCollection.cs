
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
    public class SubmodelElementCollection : SubmodelElement
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

        [XmlIgnore]
        public bool Ordered = false;

        [XmlIgnore]
        public bool AllowDuplicates = false;

        public SubmodelElementCollection()
        {
            ModelType = ModelTypes.SubmodelElementCollection;
        }

        public SubmodelElementCollection(SubmodelElement src)
            : base(src)
        {
            if (!(src is SubmodelElementCollection smc))
            {
                return;
            }

            Ordered = smc.Ordered;
            AllowDuplicates = smc.AllowDuplicates;
            ModelType = ModelTypes.SubmodelElementCollection;

            foreach (var sme in smc.Value)
            {
                Value.Add(sme);
            }
        }
    }
}
