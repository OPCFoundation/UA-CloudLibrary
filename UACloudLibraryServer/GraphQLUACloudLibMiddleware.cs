/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using GraphQL.Server.Transports.AspNetCore;
    using GraphQL.Types;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using UACloudLibrary.Interfaces;

    public class GraphQLUACloudLibMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema> where TSchema: ISchema
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
