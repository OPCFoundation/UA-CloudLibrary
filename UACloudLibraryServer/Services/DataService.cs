using System;
using System.Threading.Tasks;
using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Domain.Model;

#nullable enable

namespace HttpDataplane.Services;

public class DataService(IDataPlaneStore dataFlowStore) : IDataService
{
    public Task<bool> IsPermitted(string apiKey, DataFlow dataFlow)
    {
        return Task.FromResult(dataFlow.Destination.Properties["token"] as string == apiKey);
    }

    public async Task<DataFlow?> GetFlow(string id)
    {
        return await dataFlowStore.FindByIdAsync(id);
    }

    public async Task<DataFlow> CreatePublicEndpoint(DataFlow dataFlow)
    {
        var id = dataFlow.Id; //todo: should this be the DataAddress ID or even randomly generated?
        var apiToken = Guid.NewGuid().ToString();

        dataFlow.State = DataFlowState.Started;
        dataFlow.Destination = new DataAddress("HttpData")
        {
            Properties =
            {
                ["url"] = $"http://localhost:8080/api/v1/public/{id}",
                ["token"] = apiToken
            }
        };
        await dataFlowStore.UpsertAsync(dataFlow, true);
        return dataFlow;
    }
}
