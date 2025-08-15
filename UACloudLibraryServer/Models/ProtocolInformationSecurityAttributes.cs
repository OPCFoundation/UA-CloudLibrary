
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AdminShell
{
    [DataContract]
    public partial class ProtocolInformationSecurityAttributes
    {
        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum ProtocolInformationSecurityAttributesType
        {
            /// <summary>
            /// Enum NONEEnum for NONE
            /// </summary>
            [EnumMember(Value = "NONE")]
            NONEEnum = 0,
            /// <summary>
            /// Enum RFCTLSAEnum for RFC_TLSA
            /// </summary>
            [EnumMember(Value = "RFC_TLSA")]
            RFCTLSAEnum = 1,
            /// <summary>
            /// Enum W3CDIDEnum for W3C_DID
            /// </summary>
            [EnumMember(Value = "W3C_DID")]
            W3CDIDEnum = 2
        }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [Required]
        [DataMember(Name = "type")]
        public ProtocolInformationSecurityAttributesType? Type { get; set; }

        /// <summary>
        /// Gets or Sets Key
        /// </summary>
        [Required]
        [DataMember(Name = "key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [Required]
        [DataMember(Name = "value")]
        public string Value { get; set; }
    }
}
