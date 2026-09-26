/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using AdminShell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ServiceFilter(typeof(DppAuditFailureFilter))]
    [ApiController]
    public class ApprovalController : Controller
    {
        private readonly UAClient _client;
        private readonly IDppAuditLog _auditLog;
        private readonly ILogger _logger;

        public ApprovalController(UAClient client, IDppAuditLog auditLog, ILoggerFactory logger)
        {
            _client = client;
            _auditLog = auditLog;
            _logger = logger.CreateLogger("ApprovalController");
        }

        // The administrator performing the approval; bound to the audit entry so the created copy is
        // attributable like every other DPP create.
        private string OperatorId => User?.Identity?.Name ?? "anonymous";


        [HttpPut]
        [Route("/approval/{identifier}")]
        [Authorize(Policy = "AdministrationPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful approval.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The provided nodeset was not found.")]
        [SwaggerResponse(statusCode: 500, type: typeof(string), description: "The provided information model could not be stored or updated.")]
        public async Task<IActionResult> ApproveNameSpaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier,
            [FromQuery][Required][SwaggerParameter("(Name of the approved namespace)")] string name)
        {
            // An approval copies a nodeset, which may carry a DPP, so it is a DPP create and is
            // audited like every other one. Write the intent first: the copy touches the blob store
            // and the database, which share no transaction, so an "Attempted" entry with no matching
            // outcome is the signal that a copy may have landed without being fully logged.
            string operationId = DppAuditOperationId.New();
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Create, identifier, $"approve as {name}", "Attempted", operationId).ConfigureAwait(false);

            UploadResult result = await _client.CopyNodeset(User.Identity.Name, identifier, name).ConfigureAwait(false);

            if (result.Succeeded)
            {
                // The copy is durable, so a failure to record this outcome must not be reported as a
                // retryable refusal.
                await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Create, identifier, $"approve as {name}", "Success", operationId).ConfigureAwait(false);
                return new ObjectResult("Approval successful") { StatusCode = (int)HttpStatusCode.OK };
            }

            if (result.PartiallyApplied)
            {
                // The nodeset was stored but the operation did not complete, so the approval left
                // durable changes behind and must not be presented as a clean failure.
                await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Create, identifier, $"approve as {name}", "PartialFailure", operationId).ConfigureAwait(false);
                _logger.LogError("Approval of {Identifier} partly applied: {Message}", identifier, result.Message);

                return new ObjectResult(result.Message) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            // Nothing was written. Previously this branch was unreachable: CopyNodeset returned a
            // message string and never null, so the null check reported "Approval successful" for
            // every failure, including a missing nodeset.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Create, identifier, $"approve as {name}", "Failed", operationId).ConfigureAwait(false);
            _logger.LogError("Approval failed for {Identifier}: {Message}", identifier, result.Message);

            return new ObjectResult(result.Message) { StatusCode = (int)HttpStatusCode.NotFound };
        }
    }
}
