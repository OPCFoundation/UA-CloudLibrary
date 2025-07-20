
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AdminShell
{
    [DataContract]
    public partial class ProtocolInformation
    {
        /// <summary>
        /// Gets or Sets Href
        /// </summary>
        [Required]
        [MaxLength(2048)]
        [DataMember(Name = "href")]
        public string Href { get; set; }

        /// <summary>
        /// Gets or Sets EndpointProtocol
        /// </summary>
        [MaxLength(128)]
        [DataMember(Name = "endpointProtocol")]
        public string EndpointProtocol { get; set; }

        /// <summary>
        /// Gets or Sets EndpointProtocolVersion
        /// </summary>
        [DataMember(Name = "endpointProtocolVersion")]
        public List<string> EndpointProtocolVersion { get; set; }

        /// <summary>
        /// Gets or Sets Subprotocol
        /// </summary>

        [MaxLength(128)]
        [DataMember(Name = "subprotocol")]
        public string Subprotocol { get; set; }

        /// <summary>
        /// Gets or Sets SubprotocolBody
        /// </summary>
        [MaxLength(128)]
        [DataMember(Name = "subprotocolBody")]
        public string SubprotocolBody { get; set; }

        /// <summary>
        /// Gets or Sets SubprotocolBodyEncoding
        /// </summary>
        [MaxLength(128)]
        [DataMember(Name = "subprotocolBodyEncoding")]
        public string SubprotocolBodyEncoding { get; set; }

        /// <summary>
        /// Gets or Sets SecurityAttributes
        /// </summary>
        [DataMember(Name = "securityAttributes")]
        public List<ProtocolInformationSecurityAttributes> SecurityAttributes { get; set; }
    }
}
