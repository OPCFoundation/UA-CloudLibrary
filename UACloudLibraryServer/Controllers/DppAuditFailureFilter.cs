using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Controllers
{
    /// <summary>
    /// Translates a <see cref="DppAuditException"/> into a 503 with an EN 18222-shaped body.
    /// The audit log is a precondition for serving or changing DPP data, so when an entry cannot be
    /// committed the request must fail rather than silently complete unlogged. 503 (not 500) marks
    /// this as a retryable dependency outage rather than a defect in the caller's request.
    /// </summary>
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
