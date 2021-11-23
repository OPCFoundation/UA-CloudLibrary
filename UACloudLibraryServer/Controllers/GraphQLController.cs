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

namespace UACloudLibrary
{
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

    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    [ApiController]
    public class GraphQlController : ControllerBase
    {
        IDocumentExecuter _executer;
        ISchema _schema;
        static DocumentWriter _writer = new DocumentWriter(true);

        public GraphQlController(ISchema schema, IDocumentExecuter executer)
        {
            _schema = schema;
            _executer = executer;
        }

        [HttpGet]
        [Route("/graphql")]
        [SwaggerResponse(statusCode: 200, description: "The result of the executed GraphQL query.")]
        public async Task Get(
            [FromQuery][Required][SwaggerParameter("The GraphQL query.")] string query,
            [FromQuery][SwaggerParameter("An optional set of variables.")] string variables,
            [FromQuery][SwaggerParameter("An optional operation name.")] string operationName,
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
            public string OperationName { get; set; }

            public string Query { get; set; }

            public JObject Variables { get; set; }
        }

        async Task Execute(string query,
            string operationName,
            JObject variables,
            CancellationToken cancellation)
        {
            try
            {
                ExecutionOptions options = new ExecutionOptions()
                {
                    Schema = _schema,
                    Query = query,
                    OperationName = operationName,
                    Inputs = variables?.ToInputs(),
                    CancellationToken = cancellation,
#if DEBUG
                    ThrowOnUnhandledException = true,
                    EnableMetrics = true,
#endif
                };

                ExecutionResult result = await _executer.ExecuteAsync(options).ConfigureAwait(false);
                await _writer.WriteAsync(Response.Body, result, cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ExecutionResult result = new ExecutionResult();
                result.Errors = new ExecutionErrors();
                result.Errors.Add(new ExecutionError(ex.Message));
                await _writer.WriteAsync(Response.Body, result, cancellation).ConfigureAwait(false);
            }
        }

        static JObject ParseVariables(string variables)
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
