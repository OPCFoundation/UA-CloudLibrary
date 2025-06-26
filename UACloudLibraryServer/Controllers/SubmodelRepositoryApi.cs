
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(AuthenticationSchemes = UserService.APIAuthorizationSchemes)]
    [ApiController]
    public class SubmodelRepositoryApiController : ControllerBase
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public SubmodelRepositoryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all submodel elements including their hierarchy
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">List of found submodel elements</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements")]
        [SwaggerOperation("GetAllSubmodelElements")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<SubmodelElement>), description: "List of found submodel elements")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllSubmodelElements([FromRoute][Required] string submodelIdentifier, [FromQuery] int limit, [FromQuery] string cursor, [FromQuery] string level, [FromQuery] string extent, [FromQuery] string diff)
        {
            string decodedSubmodelIdentifier = null;
            if (submodelIdentifier != null)
            {
                decodedSubmodelIdentifier = Base64UrlEncoder.Decode(submodelIdentifier);
            }

            List<SubmodelElement> submodelElements = _aasEnvService.GetAllSubmodelElementsFromSubmodel(decodedSubmodelIdentifier);

            PagedResult<SubmodelElement> output = PagedResult<SubmodelElement>.ToPagedList(submodelElements, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns all Submodels
        /// </summary>
        /// <param name="semanticId">The value of the semantic id reference (BASE64-URL-encoded)</param>
        /// <param name="idShort">The Asset Administration Shell’s IdShort</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested Submodels</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels")]
        [SwaggerOperation("GetAllSubmodels")]
        [SwaggerResponse(statusCode: 200, type: typeof(Submodel), description: "Requested Submodels")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllSubmodels([FromQuery][StringLength(3072, MinimumLength = 1)] string semanticId, [FromQuery] string idShort, [FromQuery] int limit, [FromQuery] string cursor, [FromQuery] string level, [FromQuery] string extent)
        {
            string reqSemanticId = null;
            if (semanticId != null)
            {
                reqSemanticId = Base64UrlEncoder.Decode(semanticId);
            }

            Reference reference = new Reference { Keys = new List<Key> { new Key("Submodel", reqSemanticId) } };

            List<Submodel> submodelList = _aasEnvService.GetAllSubmodels(reference, idShort);

            PagedResult<Submodel> output = PagedResult<Submodel>.ToPagedList(submodelList, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns a specific Submodel
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested Submodel</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}")]
        [SwaggerOperation("GetSubmodelById")]
        [SwaggerResponse(statusCode: 200, type: typeof(Submodel), description: "Requested Submodel")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetSubmodelById([FromRoute][Required] string submodelIdentifier, [FromQuery] string level, [FromQuery] string extent)
        {
            string decodedSubmodelIdentifier = Base64UrlEncoder.Decode(submodelIdentifier);

            Submodel output = _aasEnvService.GetSubmodelById(decodedSubmodelIdentifier);

            return new ObjectResult(output);
        }

        /// <summary>
        /// Downloads file content from a specific submodel element from the Submodel at a specified path
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <response code="200">Requested file</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="405">Method not allowed - Download only valid for File submodel element</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/attachment")]
        [SwaggerOperation("GetFileByPath")]
        [SwaggerResponse(statusCode: 200, type: typeof(byte[]), description: "Requested file")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 405, type: typeof(Result), description: "Method not allowed - Download only valid for File submodel element")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual async Task<IActionResult> GetFileByPath([FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath)
        {
            string decodedSubmodelIdentifier = Base64UrlEncoder.Decode(submodelIdentifier);

            if (decodedSubmodelIdentifier == null)
            {
                throw new ArgumentException($"Cannot proceed as {nameof(decodedSubmodelIdentifier)} is null");
            }

            string fileName = _aasEnvService.GetFileByPath(decodedSubmodelIdentifier, idShortPath, out byte[] content, out long fileSize);

            // content-disposition so that the aasx file can be downloaded from the web browser.
            ContentDisposition contentDisposition = new() {
                FileName = fileName ?? throw new ArgumentException(nameof(fileName)),
                Inline = fileName.EndsWith(".pdf", StringComparison.InvariantCulture)
            };

            HttpContext.Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            HttpContext.Response.ContentLength = fileSize;
            if (fileName.EndsWith(".svg", StringComparison.InvariantCulture))
            {
                HttpContext.Response.ContentType = "image/svg+xml";
            }

            if (fileName.EndsWith(".pdf", StringComparison.InvariantCulture))
            {
                HttpContext.Response.ContentType = "application/pdf";
            }

            await HttpContext.Response.Body.WriteAsync(content);

            return new EmptyResult();
        }


        /// <summary>
        /// Returns a specific submodel element from the Submodel at a specified path
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}")]
        [SwaggerOperation("GetSubmodelElementByPath")]
        [SwaggerResponse(statusCode: 200, type: typeof(SubmodelElement), description: "Requested submodel element")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetSubmodelElementByPath([FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromQuery] string level, [FromQuery] string extent)
        {
            string decodedSubmodelIdentifier = null;
            if (submodelIdentifier != null)
            {
                decodedSubmodelIdentifier = Base64UrlEncoder.Decode(submodelIdentifier);
            }

            SubmodelElement output = _aasEnvService.GetSubmodelElementByPath(decodedSubmodelIdentifier, idShortPath);

            return new ObjectResult(output);
        }
    }
}
