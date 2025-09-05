using DataPlane.Sdk.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataPlane.Sdk.Core.Infrastructure;

public class NoopTokenProvider(ILogger<NoopTokenProvider> logger) : ITokenProvider
{
    public Task<string> GetTokenAsync()
    {
        logger.LogWarning("This is a No-Op token provider. It won't create a valid token, so it MUST be replaced by an actual implementation.");
        return Task.FromResult("REPLACE WITH ACTUAL TOKEN PROVIDER");
    }
}
