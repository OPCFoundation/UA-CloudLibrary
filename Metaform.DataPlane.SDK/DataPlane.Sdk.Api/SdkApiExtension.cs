using DataPlane.Sdk.Api.Authorization.DataFlows;
using DataPlane.Sdk.Core.Domain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace DataPlane.Sdk.Api;

public static class SdkApiExtension
{
    /// <summary>
    ///     Adds authorization handlers for every type of resource, such as <see cref="DataFlow" />
    /// </summary>
    /// <param name="services"></param>
    public static void AddSdkAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, DataFlowAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy("DataFlowAccess", policy =>
                policy.Requirements.Add(new DataFlowRequirement()));
    }
}
