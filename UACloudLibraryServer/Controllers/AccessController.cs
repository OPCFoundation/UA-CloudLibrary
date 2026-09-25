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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    [ServiceFilter(typeof(DppAuditFailureFilter))]
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
            // Write-ahead audit: Identity commits the role change in its own transaction, which the
            // audit append cannot join. Recording the intent first means a failed append refuses the
            // request before anything changes, and an "Attempted" entry with no matching outcome
            // flags a change that completed without being logged. See DPPLifecycleApiController.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Create, "access-rights", $"role={roleName}", "Attempted").ConfigureAwait(false);

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
            // Deleting the canonical administrator role is unrecoverable in-band. Removing it drops
            // the AspNetUserRoles links for every administrator, and recreating the role later does
            // not restore them - so the only accounts able to reach this endpoint and repair the
            // assignments would have just lost that ability. Refuse rather than offer a footgun.
            if (string.Equals(roleName, Roles.Administrator, StringComparison.OrdinalIgnoreCase))
            {
                await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"role={roleName}", "Denied").ConfigureAwait(false);
                return new ObjectResult($"The '{Roles.Administrator}' role is required for administrative access and cannot be deleted.") {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }

            IdentityRole role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
            if (role == null)
            {
                return NotFound();
            }

            // Write-ahead audit intent; see AddRoleAsync.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"role={roleName}", "Attempted").ConfigureAwait(false);

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
            // Write-ahead audit intent; see AddRoleAsync.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, "access-rights", $"grant role={roleName} to user={userId}", "Attempted").ConfigureAwait(false);

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

            // Revoking the administrator role from the last administrator is the same unrecoverable
            // lockout as deleting the role, reached by a different route: afterwards nobody can call
            // this endpoint to undo it. Revoking from one of several administrators stays allowed,
            // including for emergency revocation (EN 18239 section 6.3).
            if (string.Equals(roleName, Roles.Administrator, StringComparison.OrdinalIgnoreCase))
            {
                IList<IdentityUser> administrators = await userManager.GetUsersInRoleAsync(Roles.Administrator).ConfigureAwait(false);
                bool isAdministrator = administrators.Any(a => string.Equals(a.Id, user.Id, StringComparison.Ordinal));

                if (isAdministrator && administrators.Count <= 1)
                {
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"revoke role={roleName} from user={userId}", "Denied").ConfigureAwait(false);
                    return new ObjectResult($"Cannot revoke the '{Roles.Administrator}' role from the only remaining administrator; grant it to another account first.") {
                        StatusCode = (int)HttpStatusCode.Forbidden
                    };
                }
            }

            // Write-ahead audit intent; see AddRoleAsync. This matters most on the revocation path:
            // an emergency revocation that completed without a record would be indistinguishable
            // from one that never happened.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, "access-rights", $"revoke role={roleName} from user={userId}", "Attempted").ConfigureAwait(false);

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
