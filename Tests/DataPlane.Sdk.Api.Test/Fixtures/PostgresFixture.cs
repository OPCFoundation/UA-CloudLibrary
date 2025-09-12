using System;
using System.Threading.Tasks;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Infrastructure;
using Testcontainers.PostgreSql;

namespace DataPlane.Sdk.Api.Test.Fixtures;

public class PostgresFixture : AbstractFixture, IAsyncDisposable
{
    private static PostgreSqlContainer? _postgreSqlContainer;

    public PostgresFixture()
    {
        Context = CreateDbContext();
        var signalingService = new DataPlaneSignalingService(Context, Sdk);
        InitializeFixture(Context, signalingService).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.DisposeAsync();
            await _postgreSqlContainer.DisposeAsync();
        }
    }

    private DataFlowContext CreateDbContext()
    {
        const string dbName = "SdkApiTests";
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase(dbName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            .Build();
        _postgreSqlContainer.StartAsync().Wait();

        var port = _postgreSqlContainer.GetMappedPublicPort(5432);
        // dynamically map port to avoid conflicts
        var ctx = DataFlowContextFactory.CreatePostgres($"Host=localhost;Port={port};Database={dbName};Username=postgres;Password=postgres", "test-lock-id");
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
