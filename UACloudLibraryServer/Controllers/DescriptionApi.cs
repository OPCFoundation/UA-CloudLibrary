
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class DescriptionApiController : ControllerBase
    {
        /// <summary>
        /// Returns the self-describing information of a network resource (ServiceDescription)
        /// </summary>
        /// <response code="200">Requested Description</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        [HttpGet]
        [Route("/api/v3.0/description")]
        [SwaggerOperation("GetDescription")]
        [SwaggerResponse(statusCode: 200, type: typeof(ServiceDescription), description: "Requested Description")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        public virtual IActionResult GetDescription()
        {
            return new ObjectResult(new ServiceDescription() {
                Profiles = new List<Profile>()
                {
                    Profile.AssetAdministrationShellRepositoryServiceSpecificationV30MinimalProfileEnum,
                    Profile.SubmodelRepositoryServiceSpecificationV30MinimalProfileEnum,
                    Profile.RegistryServiceSpecificationV30AssetAdministrationShellRegistryEnum
                }
            });
        }
    }
}
