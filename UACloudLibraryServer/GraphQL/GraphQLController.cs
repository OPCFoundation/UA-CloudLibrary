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

namespace UA_CloudLibrary
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using GraphQL;
    using GraphQL.NewtonsoftJson;
    using GraphQL.Types;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Newtonsoft.Json.Linq;
    using Microsoft.AspNetCore.Authorization;
    using System.Diagnostics.CodeAnalysis;

    #region GraphQlController
    [Route("[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public class GraphQlController :
        Controller
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

        [HttpPost]
        public Task Post(
            [BindRequired, FromBody] PostBody body,
            CancellationToken cancellation)
        {
            return Execute(body.Query, body.OperationName, body.Variables, cancellation);
        }

        public class PostBody
        {
            public string? OperationName { get; set; }
            // removes warning cs8618
            [NotNull]
            public string? Query { get; set; }
            public JObject? Variables { get; set; }
        }

        [HttpGet]
        public Task Get(
            [FromQuery] string query,
            [FromQuery] string? variables,
            [FromQuery] string? operationName,
            CancellationToken cancellation)
        {
            var jObject = ParseVariables(variables);
            return Execute(query, operationName, jObject, cancellation);
        }

        async Task Execute(string query,
            string? operationName,
            JObject? variables,
            CancellationToken cancellation)
        {
            ExecutionOptions options = new ExecutionOptions()
            {
                Schema = schema,
                Query = query,
                OperationName = operationName,
                Inputs = variables?.ToInputs(),
                CancellationToken = cancellation,
#if (DEBUG)
                ThrowOnUnhandledException = true,
                EnableMetrics = true,
#endif
            };
            var executeAsync = await executer.ExecuteAsync(options);

            await writer.WriteAsync(Response.Body, executeAsync, cancellation);
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
                throw new Exception("Could not parse variables.", exception);
            }
        }
    }
    #endregion
}
