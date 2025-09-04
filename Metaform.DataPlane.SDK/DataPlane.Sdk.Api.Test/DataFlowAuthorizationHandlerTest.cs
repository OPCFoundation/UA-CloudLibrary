using System.Security.Claims;
using DataPlane.Sdk.Api.Authorization;
using DataPlane.Sdk.Api.Authorization.DataFlows;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;

namespace DataPlane.Sdk.Api.Test;

public class DataFlowAuthorizationHandlerTest
{
    private readonly DataFlowAuthorizationHandler _handler;
    private readonly Mock<IDataPlaneStore> _store;
    private readonly IConfiguration _configuration;

    public DataFlowAuthorizationHandlerTest()
    {
        _store = new Mock<IDataPlaneStore>();
        _handler = new DataFlowAuthorizationHandler(_store.Object);
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DataFlowAuthorizationHandlerTest>() // This line enables secrets.json access
            .Build();
    }


    [Fact]
    public async Task Handle_WhenParticipantIdMatchesPrincipal()
    {
        var context = CreateHandlerContext("participant1", "participant1");

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }


    [Fact]
    public async Task Handle_WhenParticipantIdDoesNotMatch_ExpectFailure()
    {
        var context = CreateHandlerContext("participant1", "participant2");

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenDataflowIsNotFound()
    {
        _store.Setup(s => s.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((DataFlow?)null);
        var context = CreateHandlerContext("participant1", "participant1", "df1");

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenParticipantOwnsDataFlow_Success()
    {
        var dataFlow = CreateDataFlow("participant1");
        _store.Setup(s => s.FindByIdAsync("df1")).ReturnsAsync(dataFlow);

        var context = CreateHandlerContext("participant1", "participant1", "df1");

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnDataflow()
    {
        var dataFlow = CreateDataFlow("another_participant");
        _store.Setup(s => s.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(dataFlow);

        var context = CreateHandlerContext("participant1", "participant1", "df1");

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeTrue();
    }

    private DataFlow CreateDataFlow(string participantContextId)
    {
        return new DataFlow(Guid.NewGuid().ToString()) {
            Source = new DataAddress("source-type"),
            Destination = new DataAddress("destination-type"),
            TransferType = nameof(FlowType.Pull),
            RuntimeId = Guid.NewGuid().ToString(),
            ParticipantId = participantContextId,
            AssetId = "test-asset",
            AgreementId = "test-agreement",
            State = DataFlowState.Received
        };
    }

    private static ClaimsPrincipal CreatePrincipal(string userId)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private AuthorizationHandlerContext CreateHandlerContext(string principal, string participantContext, string? dataFlowId = null)
    {
        var requirement = new DataFlowRequirement();
        var context = new AuthorizationHandlerContext([requirement], CreatePrincipal(principal), new ResourceTuple(participantContext, dataFlowId));
        return context;
    }
}
