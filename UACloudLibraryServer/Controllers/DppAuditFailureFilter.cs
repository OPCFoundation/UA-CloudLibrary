using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Controllers
{
    /// <summary>
    /// Translates a <see cref="DppAuditException"/> into an EN 18222-shaped error response.
    /// The audit log is a precondition for serving or changing DPP data, so when an entry cannot be
    /// committed the request must fail rather than silently complete unlogged.
    /// </summary>
    /// <remarks>
    /// Two outcomes are reported differently, because they are opposites for the client even though
    /// they look identical to the audit log:
    /// <list type="bullet">
    /// <item><description>
    /// The mutation never happened (or this was a read): 503, retryable. Nothing was applied, so
    /// repeating the request is the correct recovery.
    /// </description></item>
    /// <item><description>
    /// The mutation already committed and only its completion record failed
    /// (<see cref="DppAuditException.MutationCommitted"/>): 500, <b>not</b> retryable. Calling that
    /// "refused" would invite a retry that applies the change a second time.
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class DppAuditFailureFilter : IActionFilter, IOrderedFilter
    {
        private readonly ILogger _logger;

        public DppAuditFailureFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("DppAuditFailureFilter");
        }

        // Run before the default handling so the response shape stays consistent with the API.
        public int Order => int.MinValue;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Nothing to do before the action runs.
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context?.Exception is not DppAuditException auditException)
            {
                return;
            }

            if (auditException.MutationCommitted)
            {
                // The change is durable; only the record of its completion is missing. Say so
                // plainly rather than claiming a refusal, and do not use a retryable status: a
                // retry here would re-apply an already-applied change.
                _logger.LogCritical(
                    auditException,
                    "A DPP change was applied but its completion could not be recorded in the audit log. The write-ahead Attempted entry is the evidence that this operation needs reconciliation.");

                context.Result = new ObjectResult(new ApiResponse<object>(
                    DppApiStatusCodes.ServerInternalError,
                    payload: null,
                    result: new ApiResult(new() {
                        new ApiMessage("Error", "The change was applied, but it could not be fully recorded in the audit log. Do not retry; verify the current state before making further changes.")
                    })
                )) {
                    StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError
                };

                context.ExceptionHandled = true;
                return;
            }

            // 503 (not 500) marks this as a retryable dependency outage rather than a defect in the
            // caller's request: nothing was applied, so repeating the request is safe.
            _logger.LogError(auditException, "Refusing DPP request because it could not be audited.");

            context.Result = new ObjectResult(new ApiResponse<object>(
                DppApiStatusCodes.ServerInternalError,
                payload: null,
                result: new ApiResult(new() {
                    new ApiMessage("Error", "The request was refused because it could not be recorded in the audit log.")
                })
            )) {
                StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable
            };

            context.ExceptionHandled = true;
        }
    }
}
