using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Helpers for recording audit outcomes whose subject has already been durably applied.
    /// </summary>
    public static class DppAuditLogExtensions
    {
        /// <summary>
        /// Records the outcome of a mutation that has <b>already committed</b>, re-raising any audit
        /// failure as a <see cref="DppAuditException"/> marked
        /// <see cref="DppAuditException.MutationCommitted"/>.
        /// </summary>
        /// <remarks>
        /// The audit log cannot tell these two situations apart on its own - both are simply "the
        /// append failed" - but they are opposites from the caller's point of view. Refusing a
        /// request that never ran is safe and retryable; reporting the same refusal for a change
        /// that is already durable invites the client to repeat it. Marking the exception at the one
        /// place that knows the mutation succeeded keeps that knowledge from being guessed at later
        /// by the exception filter.
        /// </remarks>
        public static async Task RecordCommittedOutcomeAsync(
            this IDppAuditLog auditLog,
            string operatorId,
            DppAuditOperation operation,
            string dppId,
            string elementPath,
            string outcome,
            string operationId)
        {
            try
            {
                await auditLog.RecordAsync(operatorId, operation, dppId, elementPath, outcome, operationId).ConfigureAwait(false);
            }
            catch (DppAuditException ex)
            {
                throw new DppAuditException(ex.Message, ex, mutationCommitted: true);
            }
        }
    }
}
