
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AdminShell
{
    [DataContract]
    public partial class Endpoint
    {
        /// <summary>
        /// Gets or Sets _Interface
        /// </summary>
        [Required]
        [MaxLength(128)]
        [DataMember(Name = "interface")]
        public string _Interface { get; set; }

        /// <summary>
        /// Gets or Sets ProtocolInformation
        /// </summary>
        [Required]
        [DataMember(Name = "protocolInformation")]
        public ProtocolInformation ProtocolInformation { get; set; }
    }
}
