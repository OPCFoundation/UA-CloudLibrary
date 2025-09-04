using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataPlane.Sdk.Core.Data;

/// <summary>
///     Provides factory methods for creating instances of <see cref="DataFlowContext" />
///     configured for different database providers such as PostgreSQL and in-memory databases.
/// </summary>
public static class DataFlowContextFactory
{
    /// <summary>
    ///     Creates a <see cref="DbContext" /> that is based on PostgreSQL using the provided connection string
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="leaseId">The lease ID of this instance</param>
    /// <param name="autoCreate">Whether the DB and the schema should be created automatically</param>
    public static DataFlowContext CreatePostgres(string connectionString, string leaseId, bool autoCreate = false)
    {
        var options = new DbContextOptionsBuilder<DataFlowContext>()
            .UseNpgsql(connectionString)
            .Options;

        var dataFlowContext = new DataFlowContext(options, leaseId);

        if (autoCreate)
        {
            dataFlowContext.Database.EnsureCreated();
        }

        return dataFlowContext;
    }

    /// <summary>
    ///     Creates a <see cref="DbContext" /> that is based on PostgreSQL using the provided <see cref="IConfiguration" />
    ///     object from which
    ///     the connection string and the auto-create flag is taken.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="leaseId">The lease ID of this instance</param>
    public static DataFlowContext CreatePostgres(IConfiguration configuration, string lockId)
    {
        if (configuration == null)
        {
            throw new ArgumentException("configuration was null. Please pass the configuration to the factory's constructor.");
        }

        var options = new DbContextOptionsBuilder<DataFlowContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            .Options;


        var dataFlowContext = new DataFlowContext(options, lockId);

        if (bool.TryParse(configuration.GetSection("Database:AutoMigrate").Value, out var result) && result)
        {
            dataFlowContext.Database.EnsureCreated();
        }

        return dataFlowContext;
    }

    /// <summary>
    ///     Creates an implementation of the <see cref="DataFlowContext" /> that is based on the EF InMemory database.
    /// </summary>
    /// <param name="leaseId">The Lease ID of this instance</param>
    /// <param name="dbName">DB name. A random GUID is used if omitted.</param>
    /// <returns></returns>
    public static DataFlowContext CreateInMem(string leaseId, string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var context = new DataFlowContext(new DbContextOptionsBuilder<DataFlowContext>()
            .UseInMemoryDatabase(dbName)
            .Options, leaseId);

        return context;
    }
}
