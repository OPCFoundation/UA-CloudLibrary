
namespace AdminShell
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [XmlRoot(Namespace = "http://www.admin-shell.io/IEC61360/3/0")]
    public class DataSpecificationIEC61360 : ValueObject
    {
        [DataMember(Name = "dataType")]
        [XmlElement(ElementName = "dataType")]
        [MetaModelName("DataSpecificationIEC61360.DataType")]
        public string DataType { get; set; }

        [DataMember(Name = "definition")]
        [XmlArray(ElementName = "definition")]
        public List<LangStringIEC61360> Definition { get; set; }

        [DataMember(Name = "levelType")]
        [XmlArray(ElementName = "levelType")]
        public List<LevelType> LevelType { get; set; }

        [Required]
        [DataMember(Name = "preferredName")]
        [XmlArray(ElementName = "preferredName")]
        public List<LangStringIEC61360> PreferredName { get; set; }

        [DataMember(Name = "shortName")]
        [XmlArray(ElementName = "shortName")]
        public List<LangString> ShortName { get; set; }

        [DataMember(Name = "sourceOfDefinition")]
        [XmlElement(ElementName = "sourceOfDefinition")]
        [MetaModelName("DataSpecificationIEC61360.SourceOfDefinition")]
        public string SourceOfDefinition { get; set; }

        [DataMember(Name = "symbol")]
        [XmlElement(ElementName = "symbol")]
        [MetaModelName("DataSpecificationIEC61360.Symbol")]
        public string Symbol { get; set; }

        [DataMember(Name = "unit")]
        [XmlElement(ElementName = "unit")]
        [MetaModelName("DataSpecificationIEC61360.Unit")]
        public string Unit { get; set; }

        [DataMember(Name = "unitId")]
        [XmlElement(ElementName = "unitId")]
        public UnitId UnitId { get; set; }

        [DataMember(Name = "valueFormat")]
        [XmlElement(ElementName = "valueFormat")]
        [MetaModelName("DataSpecificationIEC61360.ValueFormat")]
        public string ValueFormat { get; set; }

        [DataMember(Name = "valueList")]
        [XmlElement(ElementName = "valueList")]
        public ValueList ValueList { get; set; }

        public DataSpecificationIEC61360() { }

        public DataSpecificationIEC61360(DataSpecificationIEC61360 src)
        {
            if (src.PreferredName != null)
                PreferredName = new List<LangStringIEC61360>(src.PreferredName);

            ShortName = src.ShortName;

            Unit = src.Unit;

            if (src.UnitId != null)
                UnitId = new UnitId(src.UnitId);

            ValueFormat = src.ValueFormat;

            SourceOfDefinition = src.SourceOfDefinition;

            Symbol = src.Symbol;

            DataType = src.DataType;

            if (src.Definition != null)
                Definition = new List<LangStringIEC61360>(src.Definition);
        }
    }
}
