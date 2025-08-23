using System.Net;
using System.Net.Http.Json;
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using Shouldly;
using static DataPlane.Sdk.Api.Test.TestAuthHandler;
[assembly: CollectionBehavior(MaxParallelThreads = 1)]

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

    [Fact]
    public async Task GetStateSuccess()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow());
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1/state");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStateWrongParticipantInUrlPath()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow());
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/v1/invalid-participant/dataflows/flow1/state");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStateDoesNotOwnDataFlow()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow(participantId: "another-user"));
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1/state");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStateDataFlowNotFound()
    {
        await DataFlowContext.DataFlows.AddAsync(CreateDataFlow("another-flow"));
        await DataFlowContext.SaveChangesAsync();
        var response = await HttpClient.GetAsync($"/api/v1/{TestUser}/dataflows/flow1/state");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartVerifySdkCallback()
    {
        var destinationDataAddress = new DataAddress("test-type");
        var latch = new CountdownEvent(1);
        Sdk.OnStart = flow =>
        {
            latch.Signal();
            return StatusResult<DataFlowResponseMessage>.Success(new DataFlowResponseMessage { DataAddress = flow.Destination });
        };

        var msg = new DataFlowStartMessage
        {
            ProcessId = "test-pid",
            AssetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = destinationDataAddress,
            TransferType = nameof(FlowType.Pull),
            TransferTypeDestination = "test-type"
        };
        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/", JsonContent.Create(msg));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseMessage = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        responseMessage.ShouldNotBeNull();
        responseMessage.DataAddress.ShouldBeEquivalentTo(destinationDataAddress);

        (await DataFlowContext.FindByIdAsync("test-pid")).ShouldSatisfyAllConditions(df => df!.AssetId.ShouldBe("test-asset"));
        latch.IsSet.ShouldBeTrue();
    }

    [Fact]
    public async Task ProvisionVerifySdkCallback()
    {
        var destinationDataAddress = new DataAddress("test-type");
        var latch = new CountdownEvent(1);
        Sdk.OnProvision = _ => {
            latch.Signal();
            return StatusResult<IList<ProvisionResource>>.Success([]);
        };

        var msg = new DataFlowProvisionMessage {
            ProcessId = "test-pid",
            AssetId = "test-asset",
            ParticipantId = TestUser,
            AgreementId = "test-agreement",
            SourceDataAddress = new DataAddress("test-type"),
            DestinationDataAddress = destinationDataAddress,
            TransferType = nameof(FlowType.Pull),
            TransferTypeDestination = "test-type"
        };

        var response = await HttpClient.PostAsync($"/api/v1/{TestUser}/dataflows/", JsonContent.Create(msg));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseMessage = await response.Content.ReadFromJsonAsync<DataFlowResponseMessage>();
        responseMessage.ShouldNotBeNull();
        responseMessage.DataAddress.ShouldBeEquivalentTo(destinationDataAddress);

        (await DataFlowContext.FindByIdAsync("test-pid")).ShouldSatisfyAllConditions(df => df!.AssetId.ShouldBe("test-asset"));
        latch.IsSet.ShouldBeTrue();
    }

    private static DataFlow CreateDataFlow(string id = "flow1", string participantId = TestUser)
    {
        return new DataFlow(id) {
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
}
