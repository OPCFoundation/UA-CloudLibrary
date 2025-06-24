
namespace AdminShell
{
    using Newtonsoft.Json;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum AssetKind
    {
        [EnumMember(Value = "Type")]
        [XmlEnum(Name = "Type")]
        Type = 0,

        [EnumMember(Value = "Instance")]
        [XmlEnum(Name = "Instance")]
        Instance = 1
    }
}
