using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using Moq;
using Shouldly;
using Testcontainers.PostgreSql;
using static DataPlane.Sdk.Core.Data.DataFlowContextFactory;
using static DataPlane.Sdk.Core.Domain.Model.DataFlowState;
using static DataPlane.Sdk.Core.Domain.Model.FailureReason;
using static DataPlane.Sdk.Core.Test.TestMethods;
using Void = DataPlane.Sdk.Core.Domain.Void;
[assembly: CollectionBehavior(MaxParallelThreads = 1)]

namespace DataPlane.Sdk.Core.Test;

public abstract class DataPlaneSignalingServiceTest : IDisposable
{
    private DataFlowContext _dataFlowContext = null!;
    private DataPlaneSdk _sdk = null!;
    private DataPlaneSignalingService _service = null!;

    public void Dispose()
    {
        _dataFlowContext.Database.EnsureDeleted();
        _dataFlowContext.SaveChanges();
    }

    protected void Initialize(DataFlowContext dataFlowContext)
    {
        var runtimeId = "test-lock-id";

        _dataFlowContext = dataFlowContext;
        _sdk = new DataPlaneSdk
        {
            DataFlowStore = _dataFlowContext
        };
        _service = new DataPlaneSignalingService(_dataFlowContext, _sdk, runtimeId);
    }

    [Fact]
    public async Task GetStateWhenExists()
    {
        var flow = CreateDataFlow("test-process-id", Provisioning);
        _dataFlowContext.DataFlows.Add(flow);
        await _dataFlowContext.SaveChangesAsync();
        var result = await _service.GetTransferStateAsync(flow.Id);
        result.ShouldNotBeNull();
        result.Content.ShouldBe(Provisioning);
    }

    [Fact]
    public async Task GetStateWhenNotExists()
    {
        var result = await _service.GetTransferStateAsync("non-existing-id");
        result.ShouldNotBeNull();
        result.IsSucceeded.ShouldBeFalse();
        result.Failure.ShouldNotBeNull();
        result.Failure.Reason.ShouldBe(NotFound);
    }

    [Fact]
    public async Task StartAsyncShouldReturnSuccessWhenDataFlowIsCreated()
    {
        var message = CreateStartMessage();

        var result = await _service.StartAsync(message);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.ShouldSatisfyAllConditions(() => result.Content!.DataAddress.ShouldNotBeNull());


        _dataFlowContext.ChangeTracker.HasChanges().ShouldBeFalse();
        _dataFlowContext.DataFlows.ShouldContain(x => x.Id == message.ProcessId);
    }

    [Fact]
    public async Task StartAsyncShouldReturnSuccessWhenDataFlowExists()
    {
        const string id = "test-process-id";
        var message = CreateStartMessage();
        message.ProcessId = id;

        var dataFlow = CreateDataFlow(id);
        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var result = await _service.StartAsync(message);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.ShouldSatisfyAllConditions(() => result.Content!.DataAddress.ShouldNotBeNull());

        _dataFlowContext.ChangeTracker.HasChanges().ShouldBeFalse();
        _dataFlowContext.DataFlows.ShouldContain(x => x.Id == message.ProcessId && x.State == Started);
    }

    [Fact]
    public async Task StartAsyncShouldReturnSuccessWhenDataFlowIsAlreadyStarted()
    {
        var startMessage = CreateStartMessage();
        var dataFlow = CreateDataFlow(startMessage.ProcessId, Started);
        await _dataFlowContext.AddAsync(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var mock = new Mock<Func<DataFlow, StatusResult<DataFlowResponseMessage>>>();
        _sdk.OnStart += mock.Object;

        var result = await _service.StartAsync(startMessage);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        mock.Verify(m => m.Invoke(dataFlow), Times.Never);
    }

    [Fact]
    public async Task StartAsyncShouldReturnFailureWhenSdkReportsFailure()
    {
        var startMessage = CreateStartMessage();

        var mock = new Mock<Func<DataFlow, StatusResult<DataFlowResponseMessage>>>();

        mock.Setup(m => m.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<DataFlowResponseMessage>.Conflict("error"));
        _sdk.OnStart += mock.Object;

        var result = await _service.StartAsync(startMessage);

        result.IsSucceeded.ShouldBeFalse();
        result.Failure!.Message.ShouldBe("error");

        (await _dataFlowContext.DataFlows.FindAsync(startMessage.ProcessId)).ShouldBeNull();

        mock.Verify(m => m.Invoke(It.IsAny<DataFlow>()), Times.Once);
    }

    [Fact]
    public async Task StartAsyncShouldReturnFailureWhenDataFlowIsLeased()
    {
        var startMessage = CreateStartMessage();
        var dataFlow = CreateDataFlow(startMessage.ProcessId, Started);
        var lease = new Lease
        {
            LeasedBy = "someone-else",
            LeaseDurationMillis = 60_000,
            EntityId = dataFlow.Id
        };
        await _dataFlowContext.DataFlows.AddAsync(dataFlow);
        await _dataFlowContext.Leases.AddAsync(lease);
        await _dataFlowContext.SaveChangesAsync();

        var mock = new Mock<Func<DataFlow, StatusResult<DataFlowResponseMessage>>>();
        _sdk.OnStart += mock.Object;

        var result = await _service.StartAsync(startMessage);
        result.IsSucceeded.ShouldBeFalse();
        result.Failure.ShouldNotBeNull();
        result.Failure.Reason.ShouldBe(Conflict);
        mock.Verify(m => m.Invoke(dataFlow), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsyncShouldReturnSuccessWhenNotExists()
    {
        var msg = CreateProvisionMessage();
        var provisionMock = new Mock<Func<DataFlow, StatusResult<IList<ProvisionResource>>>>();
        provisionMock.Setup(m => m.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<IList<ProvisionResource>>.Success([
                new ProvisionResource
                {
                    Flow = "flow-id",
                    Type = "test-type",
                    DataAddress = new DataAddress("test-type")
                }
            ]));
        _sdk.OnProvision = provisionMock.Object;
        var result = await _service.ProvisionAsync(msg);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        _dataFlowContext.DataFlows.ShouldContain(x => x.ResourceDefinitions.Count == 1 &&
                                                      x.State == Provisioning);
        _dataFlowContext.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task ProvisionAsyncShouldReturnSuccessWhenAlreadyExists()
    {
        var flow = CreateDataFlow("flow-id1", Provisioning);
        _dataFlowContext.DataFlows.Add(flow);
        await _dataFlowContext.SaveChangesAsync();

        var msg = CreateProvisionMessage();

        var provisionMock = new Mock<Func<DataFlow, StatusResult<IList<ProvisionResource>>>>();
        provisionMock.Setup(m => m.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<IList<ProvisionResource>>.Success([
                new ProvisionResource
                {
                    Flow = "flow-id",
                    Type = "another type",
                    DataAddress = new DataAddress("some data address type")
                }
            ]));
        _sdk.OnProvision = provisionMock.Object;

        msg.ProcessId = flow.Id;
        var result = await _service.ProvisionAsync(msg);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        _dataFlowContext.DataFlows.ShouldContain(x => x.ResourceDefinitions.Count == 1 &&
                                                      x.State == Provisioning &&
                                                      x.ResourceDefinitions.Any(pr => pr.Type == "another type"));
    }

    [Fact]
    public async Task ProvisionAsyncShouldReturnFailureWhenSdkReportsFailure()
    {
        var msg = CreateProvisionMessage();
        var provisionMock = new Mock<Func<DataFlow, StatusResult<IList<ProvisionResource>>>>();
        provisionMock.Setup(m => m.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<IList<ProvisionResource>>.FromCode(0, "test-error"));
        _sdk.OnProvision = provisionMock.Object;
        var result = await _service.ProvisionAsync(msg);
        result.IsSucceeded.ShouldBeFalse();
        result.Content.ShouldBeNull();
        result.Failure.ShouldNotBeNull();
        result.Failure.Message.ShouldBe("test-error");
        _dataFlowContext.DataFlows.ShouldBeEmpty();
        _dataFlowContext.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task ProvisionAsyncShouldMoveToNotifiedWhenNoResources()
    {
        var msg = CreateProvisionMessage();
        var result = await _service.ProvisionAsync(msg);
        result.IsSucceeded.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        _dataFlowContext.DataFlows.ShouldContain(x => x.ResourceDefinitions.Count == 0 &&
                                                      x.State == Notified);
        _dataFlowContext.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task TerminateAsyncShouldReturnSuccessWhenDataFlowExists()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test termination";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var result = await _service.TerminateAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        _dataFlowContext.DataFlows.ShouldContain(x => x.Id == dataFlowId);
    }

    [Fact]
    public async Task TerminateAsyncVerifySdkEventInvoked()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test termination";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        eventMock.Setup(f => f.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<Void>.Success(default));

        _sdk.OnTerminate += eventMock.Object;

        var result = await _service.TerminateAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Once);
    }

    [Fact]
    public async Task TerminateAsyncDataFlowNotFound()
    {
        const string dataFlowId = "test-flow-id";

        var result = await _service.TerminateAsync(dataFlowId);
        result.IsSucceeded.ShouldBeFalse();
        result.Failure!.Reason.ShouldBe(NotFound);
    }

    [Fact]
    public async Task TerminateAsyncShouldReturnFailureWhenSdkReportsFailure()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test termination";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        eventMock.Setup(f => f.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<Void>.Conflict("foobartestmessage"));

        _sdk.OnTerminate += eventMock.Object;

        var result = await _service.TerminateAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeFalse();
        result.Failure!.Message.ShouldBe("foobartestmessage");

        (await _dataFlowContext.DataFlows.FindAsync(dataFlow.Id))!.State.ShouldNotBe(Terminated);

        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Once);
    }

    [Fact]
    public async Task TerminateAsyncShouldReturnSuccessWhenAlreadyTerminated()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);
        dataFlow.State = Terminated;

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        _sdk.OnSuspend += eventMock.Object;

        var result = await _service.TerminateAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Never);
    }

    [Fact]
    public async Task TerminateAsyncShouldDeprovisionWhenInProvisioned()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);
        dataFlow.State = Provisioned;

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var result = await _service.TerminateAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        _dataFlowContext.DataFlows.ShouldContain(x => x.Id == dataFlowId && x.State == Deprovisioning);
    }

    [Fact]
    public async Task SuspendAsyncShouldReturnSuccessWhenDataFlowExists()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var result = await _service.SuspendAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        _dataFlowContext.DataFlows.ShouldContain(x => x.Id == dataFlowId);
    }

    [Fact]
    public async Task SuspendAsyncVerifySdkEventInvoked()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        eventMock.Setup(f => f.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<Void>.Success(default));

        _sdk.OnSuspend += eventMock.Object;

        var result = await _service.SuspendAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Once);
    }

    [Fact]
    public async Task SuspendAsyncDataFlowNotFound()
    {
        const string dataFlowId = "test-flow-id";

        var result = await _service.SuspendAsync(dataFlowId);
        result.IsSucceeded.ShouldBeFalse();
        result.Failure!.Reason.ShouldBe(NotFound);
    }

    [Fact]
    public async Task SuspendAsyncShouldReturnFailureWhenSdkReportsFailure()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        eventMock.Setup(f => f.Invoke(It.IsAny<DataFlow>()))
            .Returns(StatusResult<Void>.Conflict("foobartestmessage"));

        _sdk.OnSuspend += eventMock.Object;

        var result = await _service.SuspendAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeFalse();
        result.Failure!.Message.ShouldBe("foobartestmessage");

        (await _dataFlowContext.DataFlows.FindAsync(dataFlow.Id))!.State.ShouldNotBe(Suspended);

        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Once);
    }

    [Fact]
    public async Task SuspendAsyncShouldReturnSuccessWhenAlreadySuspended()
    {
        const string dataFlowId = "test-flow-id";
        const string reason = "Test Suspend";
        var dataFlow = CreateDataFlow(dataFlowId);
        dataFlow.State = Suspended;

        _dataFlowContext.DataFlows.Add(dataFlow);
        await _dataFlowContext.SaveChangesAsync();

        var eventMock = new Mock<Func<DataFlow, StatusResult<Void>>>();
        _sdk.OnSuspend += eventMock.Object;

        var result = await _service.SuspendAsync(dataFlowId, reason);

        result.IsSucceeded.ShouldBeTrue();
        eventMock.Verify(ev => ev.Invoke(dataFlow), Times.Never);
    }
}

[CollectionDefinition("SignalingService")] //parallelize tests in this collection
public class DataPlaneSignalingServiceTestGroup;

[Collection("SignalingService")]
public class InMemDataPlaneSignalingServiceTest : DataPlaneSignalingServiceTest
{
    public InMemDataPlaneSignalingServiceTest()
    {
        var ctx = CreateInMem("test-lock-id");
        Initialize(ctx);
    }
}

[Collection("SignalingService")]
public class PostgresDataPlaneSignalingServiceTest : DataPlaneSignalingServiceTest, IAsyncDisposable
{
    private static PostgreSqlContainer? _postgreSqlContainer;

    public PostgresDataPlaneSignalingServiceTest()
    {
        const string dbName = "SdkApiTests";
        if (_postgreSqlContainer == null) // create only once per test run
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithDatabase(dbName)
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithPortBinding(5432, true)
                .Build();
            _postgreSqlContainer.StartAsync().Wait();
        }

        var port = _postgreSqlContainer.GetMappedPublicPort(5432);
        // dynamically map port to avoid conflicts
        var ctx = CreatePostgres($"Host=localhost;Port={port};Database={dbName};Username=postgres;Password=postgres", "test-lock-id");
        ctx.Database.EnsureCreated();
        Initialize(ctx);
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.StopAsync();
            await _postgreSqlContainer.DisposeAsync();
        }
    }
}
