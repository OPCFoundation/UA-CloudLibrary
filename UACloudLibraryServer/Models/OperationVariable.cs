﻿
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class OperationVariable
    {
        public enum Direction
        {
            In,
            Out,
            InOut
        };

        [Required]
        [DataMember(Name = "value")]
        [XmlElement(ElementName = "value")]
        public SubmodelElement Value { get; set; }

        public OperationVariable()
        {
        }

        public OperationVariable(OperationVariable src)
        {
            Value = new SubmodelElement(src?.Value);
        }
    }
}
