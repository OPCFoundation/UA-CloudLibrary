using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace DataPlane.Sdk.Api.Test.Fixtures;

/// <summary>
///     fixture class for DPS API controller tests
/// </summary>
public class InMemoryFixture : AbstractFixture
{
    public InMemoryFixture()
    {
        Context = DataFlowContextFactory.CreateInMem("test-leaser");
        var signalingService = new DataPlaneSignalingService(Context, Sdk, "test-runtime-id");
        InitializeFixture(Context, signalingService);
        Config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<InMemoryFixture>()
            .Build();
    }
}
