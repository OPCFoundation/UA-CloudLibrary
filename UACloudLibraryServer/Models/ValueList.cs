
namespace AdminShell
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class ValueList
    {
        [Required]
        [DataMember(Name = "valueReferencePairTypes")]
        [XmlArray(ElementName = "valueReferencePairTypes")]
        public List<ValueObject> ValueReferencePairTypes { get; set; }
    }
}
