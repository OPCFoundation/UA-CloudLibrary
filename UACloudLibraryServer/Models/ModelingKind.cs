
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ModelingKind
    {
        [EnumMember(Value = "Template")]
        [XmlEnum(Name = "Template")]
        Template = 0,

        [EnumMember(Value = "Instance")]
        [XmlEnum(Name = "Instance")]
        Instance = 1
    }
}
