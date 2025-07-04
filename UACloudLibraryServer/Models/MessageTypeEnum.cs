﻿
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum MessageTypeEnum
    {
        [EnumMember(Value = "Undefined")]
        [XmlEnum(Name = "Undefined")]
        Undefined = 0,

        [EnumMember(Value = "Info")]
        [XmlEnum(Name = "Info")]
        Info = 1,

        [EnumMember(Value = "Warning")]
        [XmlEnum(Name = "Warning")]
        Warning = 2,

        [EnumMember(Value = "Error")]
        [XmlEnum(Name = "Error")]
        Error = 3,

        [EnumMember(Value = "Exception")]
        [XmlEnum(Name = "Exception")]
        Exception = 4
    }
}
