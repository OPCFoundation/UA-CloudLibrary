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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class AccessController : Controller
    {
        private readonly IDppAuditLog _auditLog;

        public AccessController(IDppAuditLog auditLog)
        {
            _auditLog = auditLog;
        }

        // The acting administrator performing the access-rights change; bound to the audit entry so
        // every change to roles/rights is attributable (EN 18239 §5.2(16)).
        private string OperatorId => User?.Identity?.Name ?? "anonymous";

        [HttpPut]
        [Route("/access/roles/{roleName}")]
        [Authorize(Policy = "AdministrationPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful addition.")]
        public async Task<IActionResult> AddRoleAsync(
            [FromRoute][Required][SwaggerParameter("Role name.")] string roleName,
            [FromServices] RoleManager<IdentityRole> roleManager
            )
        {
            IdentityResult result = await roleManager.CreateAsync(new IdentityRole { Name = roleName }).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return this.BadRequest(result);
            }

            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Create, "access-rights", $"role={roleName}", "Success").ConfigureAwait(false);
            return new ObjectResult("Role added successfully") { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpDelete]
        [Route("/access/roles/{roleName}")]
        [Authorize(Policy = "AdministrationPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful deletion.")]
        public async Task<IActionResult> DeleteRoleAsync(
            [FromRoute][Required][SwaggerParameter("Role name.")] string roleName,
            [FromServices] RoleManager<IdentityRole> roleManager
            )
        {
            IdentityRole role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return NotFound();
            }

            IdentityResult result = await roleManager.DeleteAsync(role).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return this.BadRequest(result);
            }

            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"role={roleName}", "Success").ConfigureAwait(false);
            return new ObjectResult("Role deleted successfully") { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpPut]
        [Route("/access/userRoles/{userId}/{roleName}")]
        [Authorize(Policy = "AdministrationPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful addition.")]
        public async Task<IActionResult> AddRoleToUserAsync(
            [FromRoute][Required][SwaggerParameter("User name.")] string userId,
            [FromRoute][Required][SwaggerParameter("Role name.")] string roleName,
            [FromServices] UserManager<IdentityUser> userManager
            )
        {
            IdentityUser user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound();
            }
            IdentityResult result = await userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return this.BadRequest(result);
            }

            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, "access-rights", $"grant role={roleName} to user={userId}", "Success").ConfigureAwait(false);
            return new ObjectResult("User role added successfully") { StatusCode = (int)HttpStatusCode.OK };
        }

        // EN 18239 §5.2(17)/(19) and §6.3: revoke a role from an actor (supports the documented
        // access-revocation process and emergency revocation on breach/non-compliance).
        [HttpDelete]
        [Route("/access/userRoles/{userId}/{roleName}")]
        [Authorize(Policy = "AdministrationPolicy")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful revocation.")]
        public async Task<IActionResult> RemoveRoleFromUserAsync(
            [FromRoute][Required][SwaggerParameter("User name.")] string userId,
            [FromRoute][Required][SwaggerParameter("Role name.")] string roleName,
            [FromServices] UserManager<IdentityUser> userManager
            )
        {
            IdentityUser user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound();
            }

            IdentityResult result = await userManager.RemoveFromRoleAsync(user, roleName).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return this.BadRequest(result);
            }

            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"revoke role={roleName} from user={userId}", "Success").ConfigureAwait(false);
            return new ObjectResult("User role revoked successfully") { StatusCode = (int)HttpStatusCode.OK };
        }
    }
}
