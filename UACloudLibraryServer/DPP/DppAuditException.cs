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

        public DppAuditException(string message, bool mutationCommitted)
            : base(message)
        {
            MutationCommitted = mutationCommitted;
        }

        public DppAuditException(string message, Exception innerException, bool mutationCommitted)
            : base(message, innerException)
        {
            MutationCommitted = mutationCommitted;
        }

        /// <summary>
        /// True when the change this entry was meant to record had already been durably applied
        /// before the append failed.
        /// </summary>
        /// <remarks>
        /// This separates two outcomes that are identical to the audit log but opposite to the
        /// client. A pre-mutation failure means nothing happened and retrying is correct. A
        /// post-commit failure means the change <i>is</i> applied and only its completion record is
        /// missing; telling that client the request was "refused" invites a retry that would apply
        /// the change twice. Defaults to false so any call site that does not state otherwise gets
        /// the safe, refusal interpretation.
        /// </remarks>
        public bool MutationCommitted { get; }
    }
}
