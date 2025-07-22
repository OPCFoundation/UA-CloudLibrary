
namespace AdminShell
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class Referable
    {
        [DataMember(Name = "category")]
        [XmlElement(ElementName = "category")]
        [MetaModelName("Referable.Category")]
        public string Category { get; set; }

        [DataMember(Name = "description")]
        [XmlArray(ElementName = "description")]
        public List<LangString> Description { get; set; } = new();

        [DataMember(Name = "displayName")]
        [XmlElement(ElementName = "displayName")]
        public List<LangString> DisplayName { get; set; }

        [Required]
        [DataMember(Name = "idShort")]
        [XmlElement(ElementName = "idShort")]
        [MetaModelName("Referable.IdShort")]
        public string IdShort { get; set; }

        [Required]
        [DataMember(Name = "modelType")]
        [XmlElement(ElementName = "modelType")]
        public ModelTypes ModelType { get; set; } = new();

        [DataMember(Name = "checksum")]
        [XmlElement(ElementName = "checksum")]
        [MetaModelName("Referable.Checksum")]
        public string Checksum { get; set; } = string.Empty;

        [XmlIgnore]
        public Referable Parent { get; set; }

        [XmlIgnore]
        public static string CONSTANT = "CONSTANT";

        [XmlIgnore]
        public static string Category_PARAMETER = "PARAMETER";

        [XmlIgnore]
        public static string VARIABLE = "VARIABLE";

        [XmlIgnore]
        public static string[] ReferableCategoryNames = new string[] { CONSTANT, Category_PARAMETER, VARIABLE };

        [XmlIgnore]
        public List<Extension> extension = null;

        [XmlIgnore]
        public DateTime TimeStampCreate;

        [XmlIgnore]
        public DateTime TimeStamp;

        public void setTimeStamp(DateTime timeStamp)
        {
            Referable r = this;

            do
            {
                r.TimeStamp = timeStamp;
                if (r != r.Parent)
                {
                    r = (Referable)r.Parent;
                }
                else
                    r = null;
            }
            while (r != null);
        }

        public Referable() { }

        public Referable(Referable src)
        {
            if (src == null)
                return;

            IdShort = src.IdShort;

            Category = src.Category;

            if (src.Description != null)
                Description = src.Description;
        }
    }
}
