
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum KeyElements
    {
        [EnumMember(Value = "Asset")]
        [XmlEnum(Name = "Asset")]
        Asset = 0,

        [EnumMember(Value = "AssetAdministrationShell")]
        [XmlEnum(Name = "AssetAdministrationShell")]
        AssetAdministrationShell = 1,

        [EnumMember(Value = "ConceptDescription")]
        [XmlEnum(Name = "ConceptDescription")]
        ConceptDescription = 2,

        [EnumMember(Value = "Submodel")]
        [XmlEnum(Name = "Submodel")]
        Submodel = 3,

        [EnumMember(Value = "AccessPermissionRule")]
        [XmlEnum(Name = "AccessPermissionRule")]
        AccessPermissionRule = 4,

        [EnumMember(Value = "AnnotatedRelationshipElement")]
        [XmlEnum(Name = "AnnotatedRelationshipElement")]
        AnnotatedRelationshipElement = 5,

        [EnumMember(Value = "BasicEvent")]
        [XmlEnum(Name = "BasicEvent")]
        BasicEvent = 6,

        [EnumMember(Value = "Blob")]
        [XmlEnum(Name = "Blob")]
        Blob = 7,

        [EnumMember(Value = "Capability")]
        [XmlEnum(Name = "Capability")]
        Capability = 8,

        [EnumMember(Value = "DataElement")]
        [XmlEnum(Name = "DataElement")]
        DataElement = 9,

        [EnumMember(Value = "File")]
        [XmlEnum(Name = "File")]
        File = 10,

        [EnumMember(Value = "Entity")]
        [XmlEnum(Name = "Entity")]
        Entity = 11,

        [EnumMember(Value = "Event")]
        [XmlEnum(Name = "Event")]
        Event = 12,

        [EnumMember(Value = "MultiLanguageProperty")]
        [XmlEnum(Name = "MultiLanguageProperty")]
        MultiLanguageProperty = 13,

        [EnumMember(Value = "Operation")]
        [XmlEnum(Name = "Operation")]
        Operation = 14,

        [EnumMember(Value = "Property")]
        [XmlEnum(Name = "Property")]
        Property = 15,

        [EnumMember(Value = "Range")]
        [XmlEnum(Name = "Range")]
        Range = 16,

        [EnumMember(Value = "ReferenceElement")]
        [XmlEnum(Name = "ReferenceElement")]
        ReferenceElement = 17,

        [EnumMember(Value = "RelationshipElement")]
        [XmlEnum(Name = "RelationshipElement")]
        RelationshipElement = 18,

        [EnumMember(Value = "SubmodelElement")]
        [XmlEnum(Name = "SubmodelElement")]
        SubmodelElement = 19,

        [EnumMember(Value = "SubmodelElementList")]
        [XmlEnum(Name = "SubmodelElementList")]
        SubmodelElementList = 20,

        [EnumMember(Value = "SubmodelElementStruct")]
        [XmlEnum(Name = "SubmodelElementStruct")]
        SubmodelElementStruct = 21,

        [EnumMember(Value = "View")]
        [XmlEnum(Name = "View")]
        View = 22,

        [EnumMember(Value = "GlobalReference")]
        [XmlEnum(Name = "GlobalReference")]
        GlobalReference = 23,

        [EnumMember(Value = "FragmentReference")]
        [XmlEnum(Name = "FragmentReference")]
        FragmentReference = 24,

        [EnumMember(Value = "SubmodelElementCollection")]
        [XmlEnum(Name = "SubmodelElementCollection")]
        SubmodelElementCollection = 25,

        [EnumMember(Value = "ModelReference")]
        [XmlEnum(Name = "ModelReference")]
        ModelReference = 26,

        [EnumMember(Value = "ExternalReference")]
        [XmlEnum(Name = "ExternalReference")]
        ExternalReference = 27
    }
}
