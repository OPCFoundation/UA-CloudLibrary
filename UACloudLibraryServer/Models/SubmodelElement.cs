
namespace AdminShell
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using JsonSubTypes;
    using Newtonsoft.Json;

    [DataContract]
    [JsonConverter(typeof(JsonSubtypes), "ModelType")]
    public class SubmodelElement
    {
        [DataMember(Name = "semanticId")]
        [XmlElement(ElementName = "semanticId")]
        public string SemanticId { get; set; }
    }
}

