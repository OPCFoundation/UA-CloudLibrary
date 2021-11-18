using GraphQL;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UA_CloudLibrary.GraphQL
{
    [Authorize]
    [ApiController]
    public class GraphQlController : ControllerBase
    {
#nullable enable
        IDocumentExecuter executer;
        ISchema schema;
        static DocumentWriter writer = new DocumentWriter(true);

        public GraphQlController(ISchema schema, IDocumentExecuter executer)
        {
            this.schema = schema;
            this.executer = executer;
        }

        [HttpGet]
        [Route("/graphql")]
        [SwaggerResponse(statusCode: 200, description: "The result of the executed GraphQL query.")]
        public async Task Get(
            [FromQuery][Required][SwaggerParameter("The GraphQL query.")] string query,
            [FromQuery][SwaggerParameter("An optional set of variables.")] string? variables,
            [FromQuery][SwaggerParameter("An optional operation name.")] string? operationName,
            [SwaggerParameter("An optional cancellation token.")] CancellationToken cancellation)
        {
            if (!string.IsNullOrEmpty(query))
            {
                await Execute(query, operationName, ParseVariables(variables), cancellation).ConfigureAwait(false);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        [HttpPost]
        [Route("/graphql")]
        [SwaggerResponse(statusCode: 200, description: "The result of the executed GraphQL query.")]
        public async Task Post(
            [BindRequired, FromBody][SwaggerParameter("The GraphQL query.")] PostBody body,
            [SwaggerParameter("An optional cancellation token.")] CancellationToken cancellation)
        {
            if ((body != null) && (body.Query != null))
            {
                await Execute(body.Query, body.OperationName, body.Variables, cancellation).ConfigureAwait(false);
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        public class PostBody
        {
            public string? OperationName { get; set; }

            public string? Query { get; set; }

            public JObject? Variables { get; set; }
        }

        async Task Execute(string query,
            string? operationName,
            JObject? variables,
            CancellationToken cancellation)
        {
            try
            {
                ExecutionOptions options = new ExecutionOptions()
                {
                    Schema = schema,
                    Query = query,
                    OperationName = operationName,
                    Inputs = variables?.ToInputs(),
                    CancellationToken = cancellation,
#if DEBUG
                    ThrowOnUnhandledException = true,
                    EnableMetrics = true,
#endif
                };

                ExecutionResult result = await executer.ExecuteAsync(options).ConfigureAwait(false);
                await writer.WriteAsync(Response.Body, result, cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ExecutionResult result = new ExecutionResult();
                result.Errors = new ExecutionErrors();
                result.Errors.Add(new ExecutionError(ex.Message));
                await writer.WriteAsync(Response.Body, result, cancellation).ConfigureAwait(false);
            }
        }

        static JObject? ParseVariables(string? variables)
        {
            if (variables == null)
            {
                return null;
            }

            try
            {
                return JObject.Parse(variables);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Could not parse variables.", exception);
                return null;
            }
        }
    }
}
