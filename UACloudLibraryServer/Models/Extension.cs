
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AdminShell
{
    [DataContract]
    public class Extension : HasSemantics
    {
        [Required]
        [DataMember(Name="name")]
        [XmlElement(ElementName = "name")]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name="refersTo")]
        [XmlArray(ElementName = "refersTo")]
        public List<ModelReference> RefersTo { get; set; } = new();

        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public string Value { get; set; } = string.Empty;

        [DataMember(Name = "valueType")]
        [XmlElement(ElementName = "valueType")]
        public string ValueType { get; set; } = "string";
    }
}
