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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library.Authentication
{
    /// <summary>
    /// Authorization requirement that mandates a Read-Write API key for mutating operations (POST, PUT, DELETE).
    /// </summary>
    public class ReadWriteApiKeyRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Authorization handler that enforces Read-Write API key access for mutating HTTP methods.
    /// </summary>
    public class ReadWriteApiKeyHandler : AuthorizationHandler<ReadWriteApiKeyRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ReadWriteApiKeyHandler> _logger;

        public ReadWriteApiKeyHandler(IHttpContextAccessor httpContextAccessor, ILogger<ReadWriteApiKeyHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ReadWriteApiKeyRequirement requirement)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("ReadWriteApiKeyHandler: HttpContext is null");
                context.Fail();
                return Task.CompletedTask;
            }

            string httpMethod = httpContext.Request.Method.ToUpperInvariant();

            // Only enforce Read-Write requirement for mutating operations
            if (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE")
            {
                // Check if this is API key authentication (has ApiKeyType claim)
                var apiKeyTypeClaim = context.User?.Claims.FirstOrDefault(c => c.Type == "ApiKeyType");

                if (apiKeyTypeClaim != null)
                {
                    // User authenticated with API key
                    if (apiKeyTypeClaim.Value == "Read-Write")
                    {
                        _logger.LogDebug("Read-Write API key allowed for {HttpMethod} to {Path}", httpMethod, httpContext.Request.Path);
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogWarning("Read-Only API key denied for {HttpMethod} to {Path}. User: {UserName}", httpMethod, httpContext.Request.Path, context.User?.Identity?.Name);
                        context.Fail();
                    }
                }
                else
                {
                    // Not using API key authentication (could be Basic Auth, cookies, etc.)
                    // Allow these through - they're not subject to API key type restrictions
                    _logger.LogDebug("Non-API-key authentication allowed for {HttpMethod} to {Path}", httpMethod, httpContext.Request.Path);
                    context.Succeed(requirement);
                }
            }
            else
            {
                // GET, HEAD, OPTIONS, etc. - allow all authenticated users
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
