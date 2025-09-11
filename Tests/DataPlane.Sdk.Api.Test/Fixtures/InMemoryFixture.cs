using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Infrastructure;

namespace DataPlane.Sdk.Api.Test.Fixtures;

/// <summary>
///     fixture class for DPS API controller tests
/// </summary>
public class InMemoryFixture : AbstractFixture
{
    public InMemoryFixture()
    {
        Context = DataFlowContextFactory.CreateInMem("test-leaser");
        var signalingService = new DataPlaneSignalingService(Context, Sdk);
        InitializeFixture(Context, signalingService);
    }
}