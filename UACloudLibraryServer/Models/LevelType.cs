
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum LevelType
    {
        [EnumMember(Value = "Min")]
        [XmlEnum(Name = "Min")]
        Min = 0,

        [EnumMember(Value = "Max")]
        [XmlEnum(Name = "Max")]
        Max = 1,

        [EnumMember(Value = "Nom")]
        [XmlEnum(Name = "Nom")]
        Nom = 2,

        [EnumMember(Value = "Typ")]
        [XmlEnum(Name = "Typ")]
        Typ = 3
    }
}
