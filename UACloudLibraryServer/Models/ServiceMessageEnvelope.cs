using System.Collections.Generic;
using Opc.Ua;

namespace AdminShell
{
    public class ServiceMessageEnvelope
    {
        /// <summary>
        /// The authentication header associated with the message.
        /// </summary>
        public string Authentication;

        /// <summary>
        /// The locale Ids to use when responding to the message.
        /// </summary>
        public List<string> LocaleIds;

        /// <summary>
        /// The DataTypeId for the Body.
        /// </summary>
        public ExpandedNodeId ServiceId;

        /// <summary>
        /// The message to encode or the decoded message.
        /// </summary>
        public IEncodeable Body;
    }
}
