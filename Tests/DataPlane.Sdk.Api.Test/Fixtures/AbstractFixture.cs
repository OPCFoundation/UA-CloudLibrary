using System;
using System.Net.Http;
using System.Threading.Tasks;
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;

namespace DataPlane.Sdk.Api.Test.Fixtures;

public class AbstractFixture : IDisposable
{
    private TestServer? _app;

    public HttpClient? Client { get; private set; }

    public DataFlowContext? Context { get; protected init; }

    public DataPlaneSdk Sdk { get; } = new() { RuntimeId = "test-runtime-id" };

    public void Dispose()
    {
        Client?.Dispose();
        Context?.Dispose();
    }

    internal async Task InitializeFixture(DataFlowContext context, IDataPlaneSignalingService service)
    {
        _app = await TestProgram.CreateTestServerAsync(context, service).ConfigureAwait(false);

        Client = _app.CreateClient();
    }
}
