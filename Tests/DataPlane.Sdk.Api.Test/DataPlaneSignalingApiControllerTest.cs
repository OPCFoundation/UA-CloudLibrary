using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DataPlane.Sdk.Api.Test.Fixtures;
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using Shouldly;
using static DataPlane.Sdk.Api.Test.TestAuthHandler;


namespace DataPlane.Sdk.Api.Test;

/// <summary>
///     Base class for DPS API controller tests
/// </summary>
public abstract class DataPlaneSignalingApiControllerTest(DataFlowContext dataFlowContext, HttpClient httpClient, DataPlaneSdk sdk) : IDisposable
{
    private DataFlowContext DataFlowContext { get; } = dataFlowContext;
    private HttpClient HttpClient { get; } = httpClient;
    private DataPlaneSdk Sdk { get; } = sdk;

    public void Dispose()
    {
        DataFlowContext.DataFlows.RemoveRange(DataFlowContext.DataFlows);
        DataFlowContext.Leases.RemoveRange(DataFlowContext.Leases);
        DataFlowContext.SaveChanges();
    }

    #region Suspend

    [Fact]
    public async Task SuspendSuccess()
    {
        Sdk.OnSuspend = null;
        var dataFlow = CreateDataFlow();
        dataFlow.State = DataFlowState.Started;
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/suspend", new DataFlowSuspendMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuspendNotfoundExpect404()
    {
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/not-exist/suspend", new DataFlowSuspendMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendInWrongStateExpect400()
    {
        Sdk.OnSuspend = null;
        var dataFlow = CreateDataFlow();
        dataFlow.State = DataFlowState.Preparing; // cannot transition from preparing to suspended
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/suspend", new DataFlowSuspendMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SuspendAlreadySuspendedExpect200()
    {
        Sdk.OnSuspend = null;
        var dataFlow = CreateDataFlow();
        dataFlow.State = DataFlowState.Suspended; // should be a noop
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/suspend", new DataFlowSuspendMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Terminate

    [Fact]
    public async Task TerminateSuccess()
    {
        Sdk.OnTerminate = null;
        var dataFlow = CreateDataFlow();
        dataFlow.State = DataFlowState.Started;
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/terminate", new DataFlowTerminationMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TerminateNotfoundExpect404()
    {
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/not-exist/terminate", new DataFlowTerminationMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TerminateAlreadyTerminatedExpect200()
    {
        Sdk.OnSuspend = null;
        var dataFlow = CreateDataFlow();
        dataFlow.State = DataFlowState.Terminated; // should be a noop
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/terminate", new DataFlowSuspendMessage
        {
            Reason = "test reason"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Prepare

    [Fact]
    public async Task PrepareSuccess()
    {
        Sdk.OnPrepare = null;
        var prepareMsg = new DataFlowPrepareMessage
        {
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };
        var jsonContent = JsonContent.Create(prepareMsg);
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/prepare", jsonContent);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        body.ShouldNotBeNull();
        body.State.ShouldBe(nameof(DataFlowState.Prepared));
        body.DataAddress.ShouldBeNull();
    }

    [Fact]
    public async Task PrepareWhenReturnsSyncSuccess()
    {
        Sdk.OnPrepare = flow =>
        {
            flow.State = DataFlowState.Prepared;
            return StatusResult<DataFlow>.Success(flow);
        };
        var prepareMsg = new DataFlowPrepareMessage
        {
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };
        var jsonContent = JsonContent.Create(prepareMsg);
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/prepare", jsonContent);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Location.ShouldBeNull();
        var body = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        body.ShouldNotBeNull();
        body.State.ShouldBe(nameof(DataFlowState.Prepared));
        body.DataAddress.ShouldBeNull();
    }

    [Fact]
    public async Task PrepareWhenReturnsAsyncSuccess()
    {
        Sdk.OnPrepare = dataFlow =>
        {
            dataFlow.State = DataFlowState.Preparing;
            return StatusResult<DataFlow>.Success(dataFlow);
        };
        var prepareMsg = new DataFlowPrepareMessage
        {
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };
        var jsonContent = JsonContent.Create(prepareMsg);
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/prepare", jsonContent);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldEndWith($"/api/v1/{TestUser}/dataflows/test-process");

        var body = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        body.ShouldNotBeNull();
        body.State.ShouldBe(nameof(DataFlowState.Preparing));
        body.DataAddress.ShouldBeNull();
    }

    [Fact]
    public async Task PrepareWhenSdkReturnsInvalidStateExpect400()
    {
        Sdk.OnPrepare = dataFlow =>
        {
            dataFlow.State = DataFlowState.Completed;
            return StatusResult<DataFlow>.Success(dataFlow);
        };
        var prepareMsg = new DataFlowPrepareMessage
        {
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };
        var jsonContent = JsonContent.Create(prepareMsg);
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/prepare", jsonContent);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PrepareWhenDataflowExistsExpect409()
    {
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();

        var prepareMsg = new DataFlowPrepareMessage
        {
            ProcessId = dataFlow.Id,
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };
        var jsonContent = JsonContent.Create(prepareMsg);
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/prepare", jsonContent);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private static DataFlow CreateDataFlow(string id = "flow1", string participantId = TestUser)
    {
        return new DataFlow(id)
        {
            Source = new DataAddress("test-type"),
            Destination = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Pull),
            RuntimeId = "test-runtime-id",
            ParticipantId = participantId,
            AssetId = "test-asset",
            AgreementId = "test-agreement",
            State = DataFlowState.Notified
        };
    }

    #endregion

    #region GetState

    [Fact]
    public async Task GetStateSuccess()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow());
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStateWrongParticipantInUrlPath()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow());
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/v1/invalid-participant/dataflows/flow1");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStateDoesNotOwnDataFlow()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow(participantId: "another-user"));
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStateDataFlowNotFound()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow("another-flow"));
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Start

    [Fact]
    public async Task StartSuccess()
    {
        Sdk.OnStart = null;
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var message = new DataFlowStartMessage
        {
            Id = dataFlow.Id,
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/start", message);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        body.ShouldNotBeNull();
        body.State.ShouldBe(nameof(DataFlowState.Started));
        body.DataAddress.ShouldNotBeNull();
    }

    [Fact]
    public async Task StartSdkValidationFailsExpect400()
    {
        Sdk.OnStart = null;
        Sdk.OnValidateStartMessage = _ => StatusResult.FromCode(400, "Invalid message");
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();
        var message = new DataFlowStartMessage
        {
            Id = dataFlow.Id,
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/start", message);
        Sdk.OnValidateStartMessage = null;
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldNotBeNull().ShouldContain("Invalid message");
    }

    [Fact]
    public async Task StartInvalidStateExpect400()
    {
        Sdk.OnStart = dataFlow =>
        {
            dataFlow.State = DataFlowState.Completed;
            return StatusResult<DataFlow>.Success(dataFlow);
        };
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();

        var message = new DataFlowStartMessage
        {
            Id = dataFlow.Id,
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/start", message);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartWhenSdkReturnsSyncSuccess()
    {
        Sdk.OnStart = dataFlow =>
        {
            dataFlow.State = DataFlowState.Started;
            return StatusResult<DataFlow>.Success(dataFlow);
        };
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();

        var message = new DataFlowStartMessage
        {
            Id = dataFlow.Id,
            ProcessId = "test-process",
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/start", message);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ShouldNotBeNull();
    }

    [Fact]
    public async Task StartWhenSdkReturnsAsyncSuccess()
    {
        Sdk.OnStart = dataFlow =>
        {
            dataFlow.State = DataFlowState.Starting;
            return StatusResult<DataFlow>.Success(dataFlow);
        };
        var dataFlow = CreateDataFlow();
        await DataFlowContext.DataFlows.AddAsync(dataFlow);
        await DataFlowContext.SaveChangesAsync();

        var message = new DataFlowStartMessage
        {
            ProcessId = dataFlow.Id,
            DatasetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = new DataAddress("test-type"),
            TransferType = nameof(FlowType.Push)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}/start", message);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull()
            .ToString().ShouldEndWith($"/api/v1/{TestUser}/dataflows/{dataFlow.Id}");
        var body = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        body.ShouldNotBeNull();
        body.State.ShouldBe(nameof(DataFlowState.Starting));
        body.DataAddress.ShouldNotBeNull();
    }

    #endregion
}

/// <summary>
///     uses the in-memory db context
/// </summary>
public class InMem(InMemoryFixture fixture)
    : DataPlaneSignalingApiControllerTest(fixture.Context!, fixture.Client!, fixture.Sdk), IClassFixture<InMemoryFixture>;

/// <summary>
///     uses the PostgreSQL db context
/// </summary>
public class Postgres(PostgresFixture fixture)
    : DataPlaneSignalingApiControllerTest(fixture.Context!, fixture.Client!, fixture.Sdk), IClassFixture<PostgresFixture>;
