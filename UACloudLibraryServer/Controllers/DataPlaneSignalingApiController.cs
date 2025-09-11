using System;
using System.Threading.Tasks;
using DataPlane.Sdk.Api.Authorization;
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DataPlane.Sdk.Api.Controllers;

[Authorize(Policy = "DataFlowAccess")]
[ApiController]
[Route("/api/v1/{participantContextId}/dataflows")]
public class DataPlaneSignalingApiController(
    IDataPlaneSignalingService signalingService,
    IAuthorizationService authorizationService,
    IOptions<DataPlaneSdkOptions> options)
    : ControllerBase
{
    [HttpPost("prepare")]
    public async Task<IActionResult> Prepare([FromRoute] string participantContextId, DataFlowPrepareMessage prepareMessage)
    {
        if (!(await authorizationService.AuthorizeAsync(User, new ResourceTuple(participantContextId, null), "DataFlowAccess")).Succeeded)
        {
            return Forbid();
        }

        var statusResult = await signalingService.PrepareAsync(prepareMessage);

        if (statusResult.IsSucceeded)
        {
            var dataFlow = statusResult.Content!;
            return dataFlow.State switch {
                DataFlowState.Preparing => Accepted(new Uri($"/api/v1/{participantContextId}/dataflows/{dataFlow.Id}", UriKind.Relative),
                    new DataFlowResponseMessage {
                        DataFlowId = dataFlow.Id,
                        State = dataFlow.State.ToString(),
                        DataplaneId = options.Value.DataplaneId
                    }),
                DataFlowState.Prepared => Ok(new DataFlowResponseMessage {
                    DataFlowId = dataFlow.Id,
                    DataplaneId = options.Value.DataplaneId,
                    State = dataFlow.State.ToString()
                }),
                _ => BadRequest($"DataFlow state {dataFlow.State} is not expected")
            };
        }

        return StatusCode((int)statusResult.Failure!.Reason, statusResult);
    }

    [HttpPost("{dataFlowId}/start")]
    public async Task<IActionResult> Start([FromRoute] string participantContextId, [FromRoute] string dataFlowId, DataFlowStartMessage startMessage)
    {
        if (!(await authorizationService.AuthorizeAsync(User, new ResourceTuple(participantContextId, dataFlowId), "DataFlowAccess")).Succeeded)
        {
            return Forbid();
        }


        var statusResult = await signalingService.StartAsync(startMessage);
        if (statusResult.IsSucceeded)
        {
            var dataFlow = statusResult.Content!;
            return dataFlow.State switch {
                DataFlowState.Starting => Accepted(new Uri($"/api/v1/{participantContextId}/dataflows/{dataFlow.Id}", UriKind.Relative),
                    new DataFlowResponseMessage {
                        DataFlowId = dataFlowId,
                        DataplaneId = options.Value.DataplaneId,
                        State = dataFlow.State.ToString(),
                        DataAddress = dataFlow.Destination
                    }),
                DataFlowState.Started => Ok(new DataFlowResponseMessage {
                    DataFlowId = dataFlowId,
                    DataAddress = dataFlow.Destination,
                    DataplaneId = options.Value.DataplaneId,
                    State = dataFlow.State.ToString()
                }),
                _ => BadRequest($"DataFlow state {dataFlow.State} is not expected")
            };
        }

        return StatusCode((int)statusResult.Failure!.Reason, statusResult);
    }

    [HttpPost("{dataFlowId}/suspend")]
    public async Task<IActionResult> Suspend([FromRoute] string participantContextId, [FromRoute] string dataFlowId, DataFlowSuspendMessage suspendMessage)
    {
        if (!(await authorizationService.AuthorizeAsync(User, new ResourceTuple(participantContextId, dataFlowId), "DataFlowAccess")).Succeeded)
        {
            return Forbid();
        }

        var statusResult = await signalingService.SuspendAsync(dataFlowId, suspendMessage.Reason);
        return statusResult.IsSucceeded ? Ok() : StatusCode((int)statusResult.Failure!.Reason, statusResult);
    }

    [HttpPost("{dataFlowId}/terminate")]
    public async Task<IActionResult> Terminate([FromRoute] string participantContextId, [FromRoute] string dataFlowId,
        DataFlowTerminationMessage terminateMessage)
    {
        if (!(await authorizationService.AuthorizeAsync(User, new ResourceTuple(participantContextId, dataFlowId), "DataFlowAccess")).Succeeded)
        {
            return Forbid();
        }

        var statusResult = await signalingService.TerminateAsync(dataFlowId, terminateMessage.Reason);
        return statusResult.IsSucceeded ? Ok() : StatusCode((int)statusResult.Failure!.Reason, statusResult);
    }

    [HttpGet("{dataFlowId}")]
    public async Task<IActionResult> GetStatus([FromRoute] string dataFlowId, [FromRoute] string participantContextId)
    {
        if (!(await authorizationService.AuthorizeAsync(User, new ResourceTuple(participantContextId, dataFlowId), "DataFlowAccess")).Succeeded)
        {
            return Forbid();
        }

        var state = await signalingService.GetTransferStateAsync(dataFlowId);

        return state.IsSucceeded
            ? Ok(new DataFlowStatusResponseMessage {
                Id = dataFlowId,
                State = state.Content
            })
            : StatusCode((int)state.Failure!.Reason, state);
    }
}
