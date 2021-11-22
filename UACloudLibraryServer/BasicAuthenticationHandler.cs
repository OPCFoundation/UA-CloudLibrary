
namespace UACloudLibrary
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;

        public BasicAuthenticationHandler(
            IUserService userService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string username = null;
            try
            {
                if (StringValues.IsNullOrEmpty(Request.Headers["Authorization"]))
                {
                    // check if we got a cookie instead
                    if (!StringValues.IsNullOrEmpty(Request.Headers["Cookie"]))
                    {
                        // fall back to cookie auth
                        foreach (string cookie in Request.Headers["Cookie"].ToList<string>())
                        {
                            if (cookie.StartsWith(".AspNetCore.Identity.Application="))
                            {
                                if (!await _userService.ValidateCookieAsync(cookie).ConfigureAwait(false))
                                {
                                    throw new ArgumentException("Invalid credentials");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Authentication header missing in request!");
                    }
                }
                else
                {

                    AuthenticationHeaderValue authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                    string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter)).Split(':');
                    username = credentials.FirstOrDefault();
                    string password = credentials.LastOrDefault();

                    if (!await _userService.ValidateCredentialsAsync(username, password).ConfigureAwait(false))
                    {
                        throw new ArgumentException("Invalid credentials");
                    }
                }
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
            }

            if (username == null)
            {
                username = "anonymous";
            }

            Claim[] claims = new[] {
                new Claim(ClaimTypes.Name, username)
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}