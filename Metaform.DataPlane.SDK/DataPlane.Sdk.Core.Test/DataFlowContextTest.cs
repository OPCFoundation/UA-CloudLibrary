using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DataPlane.Sdk.Core.Test;

public class DataFlowContextTest
{
    private readonly DataFlowContext _context;

    private readonly string _testLockId;

    public DataFlowContextTest()
    {
        _testLockId = "lock-" + Guid.NewGuid().ToString("N");
        _context = DataFlowContextFactory.CreateInMem(_testLockId);
    }


    [Fact]
    public async Task SaveAsync_ShouldAddNewDataFlow()
    {
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.UpsertAsync(dataFlow);

        _context.ChangeTracker.HasChanges().ShouldBeTrue();
        var entry = _context.ChangeTracker.Entries<DataFlow>().FirstOrDefault(e => e.Entity.Id == dataFlow.Id);
        entry.ShouldNotBeNull();
        entry.State.ShouldBe(EntityState.Added);

        entry.Entity.Id.ShouldBe(dataFlow.Id);
    }

    [Fact]
    public async Task SaveAsync_ShouldUpdateExistingDataFlow()
    {
        //create data flow
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.DataFlows.AddAsync(dataFlow);
        await _context.SaveChangesAsync();

        // update, call save
        dataFlow.State = DataFlowState.Completed;
        await _context.UpsertAsync(dataFlow);

        _context.ChangeTracker.HasChanges().ShouldBeTrue();
        var entry = _context.ChangeTracker.Entries<DataFlow>().FirstOrDefault(e => e.Entity.Id == dataFlow.Id);
        entry.ShouldNotBeNull();
        entry.State.ShouldBe(EntityState.Modified);
        entry.Entity.State.ShouldBe(DataFlowState.Completed);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnDataFlow_WhenExists()
    {
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.DataFlows.AddAsync(dataFlow);
        await _context.SaveChangesAsync();

        var foundFlow = await _context.FindByIdAsync(dataFlow.Id);

        foundFlow.ShouldNotBeNull();
        foundFlow.Id.ShouldBe(dataFlow.Id);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnNull_WhenNotExist()
    {
        var foundFlow = await _context.FindByIdAsync("non-existent-id");
        foundFlow.ShouldBeNull();
    }

    [Fact]
    public async Task FindByIdAndLeaseAsync_ShouldReturnDataFlow_WhenExistsAndLeaseAcquired()
    {
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.DataFlows.AddAsync(dataFlow);
        await _context.SaveChangesAsync();

        var result = await _context.FindByIdAndLeaseAsync(dataFlow.Id);
        // verify data flow
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.Id.ShouldBe(dataFlow.Id);
        _context.ChangeTracker.HasChanges().ShouldBeFalse(); // FindByIdAndLease should commit transaction

        //verify lease
        var lease = await _context.Leases.FindAsync(dataFlow.Id);
        lease.ShouldNotBeNull();
        lease.LeasedBy.ShouldBe(_testLockId);
        lease.IsExpired().ShouldBeFalse("lease should not be expired");
        DateTimeOffset.FromUnixTimeMilliseconds(lease.LeasedAt).DateTime.ShouldBeLessThan(DateTime.UtcNow);
    }

    [Fact]
    public async Task FindByIdAndLeaseAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _context.FindByIdAndLeaseAsync("not-exist");
        result.IsFailed.ShouldBeTrue();
        result.Failure!.Reason.ShouldBe(FailureReason.NotFound);
    }

    [Fact]
    public async Task FindByIdAndLeaseAsync_ShouldFail_WhenAlreadyLeased()
    {
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.DataFlows.AddAsync(dataFlow);
        await _context.Leases.AddAsync(new Lease
            {
                EntityId = dataFlow.Id,
                LeasedBy = "someone_else",
                LeasedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                LeaseDurationMillis = 60_000 // 1 minute
            }
        );
        await _context.SaveChangesAsync();

        var result = await _context.FindByIdAndLeaseAsync(dataFlow.Id);
        result.IsFailed.ShouldBeTrue();
        result.Failure!.Reason.ShouldBe(FailureReason.Conflict);
    }

    [Fact]
    public async Task FindByIdAndLeaseAsync_ShouldSucceed_WhenAlreadyLeasedBySelf()
    {
        var dataFlow = TestMethods.CreateDataFlow("test-flow-id");
        await _context.DataFlows.AddAsync(dataFlow);
        var originalLease = new Lease
        {
            EntityId = dataFlow.Id,
            LeasedBy = _testLockId,
            LeasedAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(20)).ToUnixTimeMilliseconds(),
            LeaseDurationMillis = 60_000 // 1 minute
        };
        await _context.Leases.AddAsync(originalLease);
        await _context.SaveChangesAsync();

        var result = await _context.FindByIdAndLeaseAsync(dataFlow.Id);
        _context.ChangeTracker.HasChanges().ShouldBeFalse(); // FindByIdAndLease should commit transaction
        _context.ChangeTracker.Entries<Lease>()
            .FirstOrDefault(l => l.Entity.EntityId == dataFlow.Id)
            .ShouldNotBeSameAs(originalLease);
        result.IsSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task NextNotLeased()
    {
        var f1 = TestMethods.CreateDataFlow("test-flow-id1", DataFlowState.Started);
        var f2 = TestMethods.CreateDataFlow("test-flow-id2", DataFlowState.Provisioned);
        var f3 = TestMethods.CreateDataFlow("test-flow-id3", DataFlowState.Notified);
        var f4 = TestMethods.CreateDataFlow("test-flow-id4", DataFlowState.Terminated);
        var f5 = TestMethods.CreateDataFlow("test-flow-id5", DataFlowState.Started);
        var f6 = TestMethods.CreateDataFlow("test-flow-id6", DataFlowState.Notified);
        _context.DataFlows.AddRange(f1, f2, f3, f4, f5, f6);
        await _context.SaveChangesAsync();

        var notLeased = await _context.NextNotLeased(100, DataFlowState.Started);
        notLeased.ShouldNotBeNull();
        notLeased.Count.ShouldBe(2);
        notLeased.ShouldContain(f1);
        notLeased.ShouldContain(f5);

        var notified = await _context.NextNotLeased(1, DataFlowState.Notified);
        notified.ShouldNotBeNull();
        notified.Count.ShouldBe(1);
        notified.ShouldContain(f3);
    }
}