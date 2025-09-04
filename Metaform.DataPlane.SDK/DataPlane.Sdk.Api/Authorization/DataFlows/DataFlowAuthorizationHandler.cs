using System.Security.Claims;
using DataPlane.Sdk.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DataPlane.Sdk.Api.Authorization.DataFlows;

public class DataFlowAuthorizationHandler(IDataPlaneStore store)
    : AuthorizationHandler<DataFlowRequirement, ResourceTuple>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        DataFlowRequirement requirement, ResourceTuple resource)
    {
        var (participantContextId, dataFlowId) = resource;

        // Verify that the participant context ID (from the request) matches the user ID in the claims
        var principal = context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (participantContextId != principal)
        {
            context.Fail();
            return;
        }

        // can happen on endpoints that don't target a specific dataflow (e.g. "start")
        if (dataFlowId == null)
        {
            context.Succeed(requirement);
            return;
        }

        var dataFlow = await store.FindByIdAsync(dataFlowId);
        //if null, then it's not a permission problem, but a data-flow-not-found problem
        if (dataFlow == null || dataFlow.ParticipantId == participantContextId)
        {
            context.Succeed(requirement);
            return;
        }

        // user does not own dataflow
        context.Fail();
    }
}
