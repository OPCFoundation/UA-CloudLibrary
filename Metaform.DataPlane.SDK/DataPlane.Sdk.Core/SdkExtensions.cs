using DataPlane.Sdk.Core.Domain.Interfaces;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static DataPlane.Sdk.Core.Domain.IConstants;

namespace DataPlane.Sdk.Core;

/// <summary>
///     Provides extension methods for registering SDK services with the dependency injection container.
/// </summary>
public static class SdkExtensions
{
    /// <summary>
    ///     Registers all required Data Plane SDK services with the provided <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to add the SDK services to.</param>
    /// <param name="sdk">The <see cref="DataPlaneSdk" /> instance containing configuration and dependencies.</param>
    /// <remarks>
    ///     This method configures:
    ///     <list type="bullet">
    ///         <item>Singleton registration of the token provider for authentication.</item>
    ///         <item>Transient registration of <see cref="AuthHeaderHandler" /> for HTTP request authentication.</item>
    ///         <item>HTTP client with authentication handler for outgoing requests.</item>
    ///         <item>Singleton registration of the data flow store and signaling service.</item>
    ///         <item>Transient registration of the control API service.</item>
    ///     </list>
    /// </remarks>
    public static void AddSdkServices(this IServiceCollection services, DataPlaneSdk sdk)
    {
        // configure HTTP Client for outgoing requests, both Control API and Data Plane Signaling
        services.AddSingleton(sdk.TokenProvider);
        services.AddTransient<AuthHeaderHandler>();
        services.AddHttpClient(HttpClientName)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddSingleton<IDataPlaneStore>(sdk.DataFlowStore);
        services.AddSingleton<IDataPlaneSignalingService>(new DataPlaneSignalingService(sdk.DataFlowStore, sdk, sdk.RuntimeId));
        services.AddTransient<IControlApiService, ControlApiService>();
    }
}
