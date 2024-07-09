using System;
using System.Runtime.Serialization;

namespace CESMII.OpcUa.NodeSetModel.Factory.Opc
{
    [Serializable]
    public class NodeSetResolverException : Exception
    {
        public NodeSetResolverException()
        {
        }

        public NodeSetResolverException(string message) : base(message)
        {
        }

        public NodeSetResolverException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NodeSetResolverException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
