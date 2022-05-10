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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using GraphQL.Server.Transports.AspNetCore;
    using GraphQL.Types;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Opc.Ua.Cloud.Library.Interfaces;

    public class GraphQLUACloudLibMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema> where TSchema : ISchema
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        public GraphQLUACloudLibMiddleware(
            IServiceProvider provider,
            RequestDelegate next,
            IGraphQLRequestDeserializer requestDeserializer,
            ILoggerFactory logger)
            : base(next, requestDeserializer)
        {
            _provider = provider;
            _logger = logger.CreateLogger("GraphQLUACloudLibMiddleware");
        }

        protected override CancellationToken GetCancellationToken(HttpContext context)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(base.GetCancellationToken(context), new CancellationTokenSource().Token);

            try
            {
                // verify the auth header
                if (StringValues.IsNullOrEmpty(context.Request.Headers["Authorization"]))
                {
                    // the header is missing, so figure out if we're still in the logged-in user context
                    using (IServiceScope scope = _provider.CreateScope())
                    {
                        SignInManager<IdentityUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();
                        if (signInManager.IsSignedIn(context.User))
                        {
                            // let the request through
                            return base.GetCancellationToken(context);
                        }
                        else
                        {
                            throw new ArgumentException("Authentication header missing in request!");
                        }
                    }
                }

                AuthenticationHeaderValue authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]);
                string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter)).Split(':');
                string username = credentials.FirstOrDefault();
                string password = credentials.LastOrDefault();

                using (IServiceScope scope = _provider.CreateScope())
                {
                    IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                    if (!userService.ValidateCredentialsAsync(username, password).GetAwaiter().GetResult())
                    {
                        // cancel request
                        cts.Cancel();
                        return cts.Token;
                    }
                    else
                    {
                        // let the request through
                        return base.GetCancellationToken(context);
                    }
                }
            }
            catch (Exception ex)
            {
                // cancel request
                cts.Cancel();
                _logger.LogError("Cancelling request due to exception: " + ex.Message);
                return cts.Token;
            }
        }
    }
}
