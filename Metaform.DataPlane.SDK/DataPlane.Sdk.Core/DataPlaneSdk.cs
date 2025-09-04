using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.Extensions.Logging;
using Void = DataPlane.Sdk.Core.Domain.Void;

namespace DataPlane.Sdk.Core;

using Void = Void;

public class DataPlaneSdk
{
    public Func<DataFlow, StatusResult<IList<ProvisionResource>>>? OnProvision;
    public Func<DataFlow, StatusResult<Void>>? OnRecover;
    public Func<DataFlow, StatusResult<DataFlowResponseMessage>>? OnStart;
    public Func<DataFlow, StatusResult<Void>>? OnSuspend;
    public Func<DataFlow, StatusResult<Void>>? OnTerminate;
    public Func<DataFlowStartMessage, StatusResult<Void>>? OnValidateStartMessage;

    //todo: make the lease id configurable
    public DataFlowContext DataFlowStore { get; set; } = DataFlowContextFactory.CreateInMem("test-lock-id");
    public string RuntimeId { get; set; } = Guid.NewGuid().ToString();
    public ITokenProvider TokenProvider { get; set; } = new NoopTokenProvider(LoggerFactory.Create(_ => { }).CreateLogger<NoopTokenProvider>());

    public static SdkBuilder Builder()
    {
        return new SdkBuilder();
    }

    internal StatusResult<Void> InvokeTerminate(DataFlow df)
    {
        return OnTerminate != null ? OnTerminate(df) : StatusResult<Void>.Success(default);
    }

    internal StatusResult<Void> InvokeSuspend(DataFlow df)
    {
        return OnSuspend != null ? OnSuspend(df) : StatusResult<Void>.Success(default);
    }

    internal StatusResult<DataFlowResponseMessage> InvokeStart(DataFlow df)
    {
        return OnStart != null
            ? OnStart(df)
            : StatusResult<DataFlowResponseMessage>.Success(new DataFlowResponseMessage {
                DataAddress = df.Destination
            });
    }

    internal StatusResult<Void> InvokeValidate(DataFlowStartMessage startMessage)
    {
        return OnValidateStartMessage?.Invoke(startMessage) ?? StatusResult<Void>.Success(default);
    }

    internal StatusResult<IList<ProvisionResource>> InvokeOnProvision(DataFlow flow)
    {
        return OnProvision != null ? OnProvision(flow) : StatusResult<IList<ProvisionResource>>.Success(Array.Empty<ProvisionResource>());
    }

    public class SdkBuilder
    {
        private readonly DataPlaneSdk _dataPlaneSdk = new() {
            OnStart = _ => StatusResult<DataFlowResponseMessage>.Success(null)
        };

        public SdkBuilder Store(DataFlowContext dataPlaneStatefulEntityStore)
        {
            _dataPlaneSdk.DataFlowStore = dataPlaneStatefulEntityStore;
            return this;
        }

        public DataPlaneSdk Build()
        {
            return _dataPlaneSdk;
        }

        public SdkBuilder OnStart(Func<DataFlow, StatusResult<DataFlowResponseMessage>> processor)
        {
            _dataPlaneSdk.OnStart = processor;
            return this;
        }

        public SdkBuilder OnProvision(Func<DataFlow, StatusResult<IList<ProvisionResource>>> processor)
        {
            _dataPlaneSdk.OnProvision = processor;
            return this;
        }

        public SdkBuilder OnTerminate(Func<DataFlow, StatusResult<Void>> processor)
        {
            _dataPlaneSdk.OnTerminate = processor;
            return this;
        }

        public SdkBuilder OnSuspend(Func<DataFlow, StatusResult<Void>> processor)
        {
            _dataPlaneSdk.OnSuspend = processor;
            return this;
        }

        public SdkBuilder OnRecover(Func<DataFlow, StatusResult<Void>> processor)
        {
            _dataPlaneSdk.OnRecover = processor;
            return this;
        }

        public SdkBuilder OnValidateStartMessage(Func<DataFlowStartMessage, StatusResult<Void>> processor)
        {
            _dataPlaneSdk.OnValidateStartMessage = processor;
            return this;
        }

        public SdkBuilder RuntimeId(string runtimeId)
        {
            _dataPlaneSdk.RuntimeId = runtimeId;
            return this;
        }
    }
}
