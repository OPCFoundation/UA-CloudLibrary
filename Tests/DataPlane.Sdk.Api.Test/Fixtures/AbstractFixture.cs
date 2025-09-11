using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DataPlane.Sdk.Api.Test.Fixtures;

public class AbstractFixture : IDisposable
{
    private WebApplicationFactory<Program>? _factory;
    public HttpClient? Client { get; private set; }
    public DataFlowContext? Context { get; protected init; }

    public DataPlaneSdk Sdk { get; } = new()
    {
        RuntimeId = "test-runtime-id"
    };

    public void Dispose()
    {
        _factory?.Dispose();
        Client?.Dispose();
        Context?.Dispose();
    }

    internal void InitializeFixture(DataFlowContext context, IDataPlaneSignalingService service)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDataPlaneStore>(context);
                services.AddSingleton(service);
            });
        });
        Client = _factory.CreateClient();
    }
}