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

namespace Opc.Ua.Cloud.Library.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using Opc.Ua.Cloud.Library.Interfaces;

    public class SignedInUserAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public SignedInUserAuthenticationHandler(
            SignInManager<IdentityUser> signInManager,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _signInManager = signInManager;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (!_signInManager.IsSignedIn(Request.HttpContext.User))
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }
                // Allow a previously authenticated, signed in user (for example via ASP.Net cookies from the graphiql browser)
                var principal2 = new ClaimsPrincipal(Request.HttpContext.User.Identity);
                var ticket2 = new AuthenticationTicket(principal2, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket2));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail($"Error during Authentication: {ex.Message}"));
            }
        }
    }
}
