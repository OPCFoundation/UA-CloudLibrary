using System.Text.Json;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DataPlane.Sdk.Core.Data;

/// <summary>
///     Represents the Entity Framework database context for managing <see cref="DataFlow" /> and <see cref="Lease" />
///     entities,
///     providing methods for data persistence, leasing, and querying data flows in a data plane store.
/// </summary>
/// <remarks>
///     This context implements <see cref="IDataPlaneStore" /> and handles concurrency using a leasing mechanism
///     to ensure exclusive access to <see cref="DataFlow" /> entities during updates.
/// </remarks>
public class DataFlowContext : DbContext, IDataPlaneStore
{
    private static readonly TimeSpan DefaultLeaseTime = TimeSpan.FromSeconds(60);
    private readonly string _lockId;

    internal DataFlowContext(DbContextOptions<DataFlowContext> options, string lockId)
        : base(options)
    {
        _lockId = lockId;
    }

    public DbSet<DataFlow> DataFlows { get; set; }
    public DbSet<Lease> Leases { get; set; }

    /// Asynchronously saves the specified
    /// <see cref="DataFlow" />
    /// instance to the data store.
    /// Acquires a lease for the given DataFlow, updates it if it exists, or adds it if it does not.
    /// Releases the lease after the operation completes. Does NOT save changes on the db context!
    /// <param name="dataflow">The <see cref="DataFlow" /> instance to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task UpsertAsync(DataFlow dataflow)
    {
        var lease = await AcquireLeaseAsync(dataflow.Id);
        var found = await DataFlows.FindAsync(dataflow.Id);
        if (found != null)
        {
            DataFlows.Entry(found).CurrentValues.SetValues(dataflow);
        }
        else
        {
            DataFlows.Add(dataflow);
        }

        await FreeLeaseAsync(lease.EntityId);
    }


    public async Task<DataFlow?> FindByIdAsync(string id)
    {
        var df = await DataFlows.FindAsync(id);
        return df;
    }

    /// Attempts to find a
    /// <see cref="DataFlow" />
    /// entity by its ID and acquire a lease on it.
    /// <param name="id">The unique identifier of the <see cref="DataFlow" /> to find and lease.</param>
    /// <returns>
    ///     A <see cref="StatusResult{DataFlow}" /> indicating the outcome:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><c>Success</c>: The entity was found and the lease was successfully acquired.</description>
    ///         </item>
    ///         <item>
    ///             <description><c>NotFound</c>: No entity with the specified identifier exists.</description>
    ///         </item>
    ///         <item>
    ///             <description><c>Conflict</c>: The entity is already leased by another process.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public async Task<StatusResult<DataFlow>> FindByIdAndLeaseAsync(string id)
    {
        var flow = await FindByIdAsync(id);
        if (flow == null)
        {
            return StatusResult<DataFlow>.NotFound();
        }

        try
        {
            await AcquireLeaseAsync(flow.Id);
            await SaveChangesAsync(); // commit transaction
            return StatusResult<DataFlow>.Success(flow);
        }
        catch (ArgumentException e)
        {
            return StatusResult<DataFlow>.Conflict($"Entity {id} is already leased by another process: {e.Message}");
        }
    }

    /// Asynchronously retrieves a collection of
    /// <see cref="DataFlow" />
    /// objects that are in the specified states and have not been leased,
    /// up to the specified maximum number.
    /// <param name="max">The maximum number of <see cref="DataFlow" /> objects to return.</param>
    /// <param name="states">
    ///     An array of <see cref="DataFlowState" /> values to filter the <see cref="DataFlow" /> objects by
    ///     state.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a collection of <see cref="DataFlow" />
    ///     objects
    ///     matching the specified states and not currently leased.
    /// </returns>
    public async Task<ICollection<DataFlow>> NextNotLeased(int max, params DataFlowState[] states)
    {
        var filteredFlows = DataFlows.Where(dataFlow => states.Contains(dataFlow.State)).Take(max);
        return await filteredFlows.ToListAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataFlow>()
            .HasKey(df => df.Id);

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.Id)
            .IsRequired();

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.State)
            .IsRequired();

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.UpdatedAt)
            .IsRequired();
        modelBuilder.Entity<DataFlow>()
            .Property(df => df.Source)
            .HasConversion(da => ToJson(da),
                s => JsonSerializer.Deserialize<DataAddress>(s, null as JsonSerializerOptions)!);

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.Destination)
            .HasConversion(da => ToJson(da),
                s => JsonSerializer.Deserialize<DataAddress>(s, null as JsonSerializerOptions)!);

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.TransferType)
            .IsRequired();

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.Properties)
            .HasConversion(
                props => ToJson(props),
                json => JsonSerializer.Deserialize<IDictionary<string, string>>(json, null as JsonSerializerOptions) ?? new Dictionary<string, string>());

        modelBuilder.Entity<DataFlow>()
            .Property(df => df.ResourceDefinitions)
            .HasConversion(
                props => ToJson(props),
                json => JsonSerializer.Deserialize<List<ProvisionResource>>(json, null as JsonSerializerOptions) ?? new List<ProvisionResource>());


        modelBuilder.Entity<Lease>()
            .HasKey(l => l.EntityId);
    }

    private static string ToJson(dynamic da)
    {
        return JsonSerializer.Serialize(da);
    }

    private async Task FreeLeaseAsync(string leaseId)
    {
        var lease = (await Leases.FindAsync(leaseId))!; //track if not tracked
        Leases.Remove(lease);
    }

    private async Task<Lease> AcquireLeaseAsync(string entityId)
    {
        return await AcquireLeaseAsync(entityId, _lockId, DefaultLeaseTime);
    }

    private async Task<Lease> AcquireLeaseAsync(string entityId, string lockId, TimeSpan leaseDuration)
    {
        var lease = new Lease {
            EntityId = entityId,
            LeasedBy = lockId,
            LeasedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LeaseDurationMillis = (long)leaseDuration.TotalMilliseconds
        };
        if (!await IsLeasedAsync(entityId))
        {
            await Leases.AddAsync(lease);
        }
        else if (await IsLeasedByAsync(entityId, lockId))
        {
            // load tracked entity and update its values
            var existing = await Leases.FindAsync(entityId);
            Entry(existing!).CurrentValues.SetValues(lease);
        }
        else
        {
            throw new ArgumentException("Cannot acquire lease, entity ${entityId} is already leased by another process.");
        }

        return lease;
    }

    private async Task<bool> IsLeasedByAsync(string entityId, string lockId)
    {
        var lease = await Leases.FindAsync(entityId);
        return lease != null && !lease.IsExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) && lease.LeasedBy == lockId;
    }

    private async Task<bool> IsLeasedAsync(string entityId)
    {
        var lease = await Leases.FindAsync(entityId);
        return lease != null && !lease.IsExpired(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}
