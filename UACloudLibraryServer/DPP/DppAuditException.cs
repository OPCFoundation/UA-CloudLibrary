using System;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Raised when a required DPP audit entry cannot be durably committed. Callers must let this
    /// propagate (or translate it into a failed response) rather than continuing: completing an
    /// access or modification that could not be logged would silently forfeit the non-repudiation
    /// guarantee that EN 18246 §4.7 requires.
    /// </summary>
    public class DppAuditException : Exception
    {
        public DppAuditException()
        {
        }

        public DppAuditException(string message)
            : base(message)
        {
        }

        public DppAuditException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
