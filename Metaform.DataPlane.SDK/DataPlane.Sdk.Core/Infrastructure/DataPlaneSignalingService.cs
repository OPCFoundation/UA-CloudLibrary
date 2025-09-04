using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using Void = DataPlane.Sdk.Core.Domain.Void;

namespace DataPlane.Sdk.Core.Infrastructure;

/// <summary>
///     DataPlane Signaling (DPS) Service implementation to handle DPS requests and events.
/// </summary>
/// <param name="dataFlowContext">
///     The underlying persistence layer for <see cref="DataFlow" /> objects. Note that this
///     service acts as transaction boundary.
/// </param>
/// <param name="sdk">An instance of the <see cref="DataPlaneSdk" /> to invoke callbacks</param>
/// <param name="runtimeId">The Runtime ID of this data plane.</param>
public class DataPlaneSignalingService(DataFlowContext dataFlowContext, DataPlaneSdk sdk, string runtimeId) : IDataPlaneSignalingService
{
    public async Task<StatusResult<DataFlowResponseMessage>> StartAsync(DataFlowStartMessage message)
    {
        var existingFlowResult = await dataFlowContext.FindByIdAndLeaseAsync(message.ProcessId);

        if (existingFlowResult.IsFailed)
        {
            if (existingFlowResult.Failure?.Reason == FailureReason.NotFound) // create new data flow
            {
                var dataFlow = CreateDataFlow(message);
                var sdkResult = sdk.InvokeStart(dataFlow);

                if (sdkResult.IsFailed)
                {
                    return StatusResult<DataFlowResponseMessage>.Failed(sdkResult.Failure!);
                }

                await dataFlowContext.UpsertAsync(dataFlow);
                await dataFlowContext.SaveChangesAsync();
                return sdkResult;
            }

            return StatusResult<DataFlowResponseMessage>.Failed(existingFlowResult.Failure!);
        }

        var existingFlow = existingFlowResult.Content!;
        if (existingFlow.State == DataFlowState.Started)
        {
            return StatusResult<DataFlowResponseMessage>.Success(CreateResponse(existingFlow));
        }

        //update existing data flow
        existingFlow.Start();
        dataFlowContext.DataFlows.Update(existingFlow);
        await dataFlowContext.SaveChangesAsync();

        return StatusResult<DataFlowResponseMessage>.Success(CreateResponse(existingFlow));
    }

    public async Task<StatusResult<Void>> SuspendAsync(string dataFlowId, string? reason = null)
    {
        var res = await dataFlowContext.FindByIdAndLeaseAsync(dataFlowId);
        if (res.IsFailed)
        {
            return StatusResult<Void>.Failed(res.Failure!);
        }

        var df = res.Content!;
        if (df.State == DataFlowState.Suspended) //de-duplication check
        {
            return StatusResult<Void>.Success(default);
        }

        var sdkResult = sdk.InvokeSuspend(df);
        if (sdkResult.IsFailed)
        {
            return StatusResult<Void>.Failed(sdkResult.Failure!);
        }

        df.Suspend(reason);
        await dataFlowContext.UpsertAsync(df);
        await dataFlowContext.SaveChangesAsync(); // commit transaction
        return sdkResult;
    }

    public async Task<StatusResult<Void>> TerminateAsync(string dataFlowId, string? reason = null)
    {
        var res = await dataFlowContext.FindByIdAndLeaseAsync(dataFlowId);
        if (res.IsFailed)
        {
            return StatusResult<Void>.Failed(res.Failure!);
        }

        var df = res.Content!;

        if (df.State == DataFlowState.Terminated) //de-duplication check
        {
            return StatusResult<Void>.Success(default);
        }

        var sdkResult = sdk.InvokeTerminate(df);
        if (sdkResult.IsFailed)
        {
            return StatusResult<Void>.Failed(sdkResult.Failure!);
        }

        if (df.State == DataFlowState.Provisioned)
        {
            df.Deprovision();
        }
        else
        {
            df.Terminate();
        }

        await dataFlowContext.UpsertAsync(df);
        await dataFlowContext.SaveChangesAsync(); //commit transaction
        return sdkResult;
    }

    public async Task<StatusResult<DataFlowState>> GetTransferStateAsync(string processId)
    {
        var flow = await dataFlowContext.DataFlows.FindAsync(processId);
        return flow == null ? StatusResult<DataFlowState>.NotFound() : StatusResult<DataFlowState>.Success(flow.State);
    }

    public Task<StatusResult<Void>> ValidateStartMessageAsync(DataFlowStartMessage startMessage)
    {
        // delegate validation to the SDK callback
        return Task.FromResult(sdk.InvokeValidate(startMessage));
    }

    public async Task<StatusResult<DataFlowResponseMessage>> ProvisionAsync(DataFlowProvisionMessage provisionMessage)
    {
        var flow = CreateDataFlow(provisionMessage);
        var result = sdk.InvokeOnProvision(flow);

        if (result.IsFailed)
        {
            return StatusResult<DataFlowResponseMessage>.Failed(result.Failure!);
        }

        var resources = result.Content;
        if (resources == null || resources.Count == 0)
        {
            flow.Notified();
        }
        else
        {
            flow.AddResourceDefinitions(resources);
            flow.Provisioning();
        }

        await dataFlowContext.UpsertAsync(flow);
        await dataFlowContext.SaveChangesAsync();
        return StatusResult<DataFlowResponseMessage>.Success(CreateResponse(flow));
    }

    private DataFlow CreateDataFlow(DataFlowProvisionMessage message)
    {
        return new DataFlow(message.ProcessId) {
            Source = message.SourceDataAddress,
            Destination = message.DestinationDataAddress,
            TransferType = message.TransferType,
            RuntimeId = runtimeId,
            ParticipantId = message.ParticipantId,
            AssetId = message.AssetId,
            AgreementId = message.AgreementId,
            CallbackAddress = message.CallbackAddress,
            Properties = message.Properties,
            State = DataFlowState.Notified
        };
    }

    private DataFlowResponseMessage CreateResponse(DataFlow existingFlow)
    {
        return new DataFlowResponseMessage {
            DataAddress = existingFlow.Destination
        };
    }

    private DataFlow CreateDataFlow(DataFlowStartMessage message)
    {
        return new DataFlow(message.ProcessId) {
            Source = message.SourceDataAddress,
            Destination = message.DestinationDataAddress,
            TransferType = message.TransferType,
            RuntimeId = runtimeId,
            ParticipantId = message.ParticipantId,
            AssetId = message.AssetId,
            AgreementId = message.AgreementId,
            CallbackAddress = message.CallbackAddress,
            Properties = message.Properties,
            State = DataFlowState.Notified
        };
    }
}
