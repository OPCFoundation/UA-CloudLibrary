
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AdminShell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace IO.Swagger.Controllers
{
    [Authorize(AuthenticationSchemes = UserService.APIAuthorizationSchemes)]
    [ApiController]
    public class ConceptDescriptionRepositoryApiController : ControllerBase
    {

        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public ConceptDescriptionRepositoryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all Concept Descriptions
        /// </summary>
        /// <param name="idShort">The Concept Description’s IdShort</param>
        /// <param name="isCaseOf">IsCaseOf reference (UTF8-BASE64-URL-encoded)</param>
        /// <param name="dataSpecificationRef">DataSpecification reference (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Concept Descriptions</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/concept-descriptions")]
        [SwaggerOperation("GetAllConceptDescriptions")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<ConceptDescription>), description: "Requested Concept Descriptions")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllConceptDescriptions([FromQuery] string idShort, [FromQuery] string isCaseOf, [FromQuery] string dataSpecificationRef, [FromQuery] int limit, [FromQuery] string cursor)
        {
            string reqIsCaseOf = null;
            if (!string.IsNullOrEmpty(isCaseOf))
            {
                reqIsCaseOf = Base64UrlEncoder.Decode(isCaseOf);
            }

            string reqDataSpecificationRef = null;
            if (!string.IsNullOrEmpty(dataSpecificationRef))
            {
                reqDataSpecificationRef = Base64UrlEncoder.Decode(dataSpecificationRef);
            }

            List<ConceptDescription> cdList = new();
            cdList = _aasEnvService.GetAllConceptDescriptions(idShort, reqIsCaseOf, reqDataSpecificationRef);

            PagedResult<ConceptDescription> output = PagedResult<ConceptDescription>.ToPagedList(cdList, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns a specific Concept Description
        /// </summary>
        /// <param name="cdIdentifier">The Concept Description’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Concept Description</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("concept-descriptions/{cdIdentifier}")]
        [SwaggerOperation("GetConceptDescriptionById")]
        [SwaggerResponse(statusCode: 200, type: typeof(ConceptDescription), description: "Requested Concept Description")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetConceptDescriptionById([FromRoute][Required] string cdIdentifier)
        {
            string decodedCdIdentifier = Base64UrlEncoder.Decode(cdIdentifier);

            ConceptDescription output = _aasEnvService.GetConceptDescriptionById(decodedCdIdentifier);

            return new ObjectResult(output);
        }
    }
}
