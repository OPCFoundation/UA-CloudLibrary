using System.Net.Http.Headers;
using DataPlane.Sdk.Core.Domain.Interfaces;

namespace DataPlane.Sdk.Core.Infrastructure;

/// <summary>
///     Handler to insert an <c>Authorization</c> header into an outgoing HTTP request using the "Bearer" prefix. It does
///     that by invoking the <see cref="ITokenProvider" />
/// </summary>
/// <param name="tokenProvider">An instance of the <see cref="ITokenProvider" /> interface to generate/obtain auth tokens</param>
public class AuthHeaderHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
