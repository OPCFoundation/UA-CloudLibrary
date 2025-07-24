using System.Collections.Generic;
using Opc.Ua;

namespace AdminShell
{
    public class ServiceMessageEnvelope
    {
        /// <summary>
        /// The authentication header associated with the message.
        /// </summary>
        public string Authentication { get; set; }

        /// <summary>
        /// The locale Ids to use when responding to the message.
        /// </summary>
        public List<string> LocaleIds { get; set; }

        /// <summary>
        /// The DataTypeId for the Body.
        /// </summary>
        public ExpandedNodeId ServiceId { get; set; }

        /// <summary>
        /// The message to encode or the decoded message.
        /// </summary>
        public IEncodeable Body { get; set; }
    }
}
