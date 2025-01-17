/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(AuthenticationSchemes = UserService.APIAuthorizationSchemes)]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        private readonly IDatabase _database;
        private readonly ILogger _logger;

        public ApprovalController(IDatabase database, ILoggerFactory logger)
        {
            _database = database;
            _logger = logger.CreateLogger("ApprovalController");
        }


        [HttpPut]
        [Route("/approval/{identifier}")]
        [Authorize(Policy = "ApprovalPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful approval.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The provided nodeset was not found.")]
        [SwaggerResponse(statusCode: 500, type: typeof(string), description: "The provided information model could not be stored or updated.")]
        public async Task<IActionResult> ApproveNameSpaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier,
            [FromQuery][SwaggerParameter("Status of the approval")] ApprovalStatus status,
            [FromQuery][SwaggerParameter("Information about the approval")] string approvalInformation)
        {
            if (await _database.ApproveNamespaceAsync(identifier, status, approvalInformation, null).ConfigureAwait(false) != null)
            {
                return new ObjectResult("Approval status updated successfully") { StatusCode = (int)HttpStatusCode.OK };
            }
            _logger.LogError($"Approval failed: {identifier} not found.");
            return NotFound();
        }
    }
}
