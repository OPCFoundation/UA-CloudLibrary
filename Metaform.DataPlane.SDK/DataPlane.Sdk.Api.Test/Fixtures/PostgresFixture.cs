using System;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace DataPlane.Sdk.Api.Test.Fixtures;

public class PostgresFixture : AbstractFixture, IAsyncDisposable
{
    private static PostgreSqlContainer? _postgreSqlContainer;

    public PostgresFixture()
    {
        Context = CreateDbContext();
        var signalingService = new DataPlaneSignalingService(Context, Sdk, "test-runtime-id");
        InitializeFixture(Context, signalingService);
        Config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<InMemoryFixture>()
            .Build();
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
        const string dbName = "DataPlaneSdkApiTests";

        string? xxx = Config.GetValue<string>("PostgresHost");
        // Obtain connection string information from the environment
        string? host = Environment.GetEnvironmentVariable("PostgresHost");
        string? user = Environment.GetEnvironmentVariable("PostgresUser");
        string? password = Environment.GetEnvironmentVariable("PostgresPass");

        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase(dbName)
            .WithUsername(user)
            .WithPassword(password)
            .WithPortBinding(5432, true)
            .Build();
        _postgreSqlContainer.StartAsync().Wait();

        var port = _postgreSqlContainer.GetMappedPublicPort(5432);
        // dynamically map port to avoid conflicts
        var ctx = DataFlowContextFactory.CreatePostgres($"Host={host};Port={port};Database={dbName};Username={user};Password={password}", "test-lock-id");
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
