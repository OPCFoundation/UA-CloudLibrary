using System.Text;
using DataPlane.Sdk.Api;
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using DataPlane.Sdk.Core.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using static DataPlane.Sdk.Core.Data.DataFlowContextFactory;
using Void = DataPlane.Sdk.Core.Domain.Void;

namespace DataPlane.Sdk.Example.Web;

public static class Extensions
{
    public static void AddDataPlaneSdk(this IServiceCollection services, IConfiguration configuration)
    {
        // initialize and configure the DataPlaneSdk
        var config = configuration.GetSection("DataPlaneSdk").Get<DataPlaneSdkOptions>() ?? throw new ArgumentException("Configuration invalid!");
        var sdk = new DataPlaneSdk {
            DataFlowStore = CreateInMem("example-leaser"),
            RuntimeId = config.RuntimeId,
            OnStart = f => StatusResult<DataFlowResponseMessage>.Success(new DataFlowResponseMessage { DataAddress = f.Destination }),
            OnRecover = _ => StatusResult<Void>.Success(default),
            OnTerminate = _ => StatusResult<Void>.Success(default),
            OnSuspend = _ => StatusResult<Void>.Success(default),
            OnProvision = f => StatusResult<IList<ProvisionResource>>.Success([])
        };

        // read required configuration from appsettings.json to make it injectable
        services.Configure<ControlApiOptions>(configuration.GetSection("DataPlaneSdk:ControlApi"));

        // add SDK core services
        services.AddSdkServices(sdk);

        // Use JWT Bearer authentication for the SDK API calls. 
        ConfigureExampleJwtAuthentication(services, configuration);


        // overwrite SDK authentication with KeycloakJWT. Effectively, this sets the default authentication scheme to "KeycloakJWT",
        // foregoing the SDK default authentication scheme and using Keycloak as the identity provider. For this to work, Keycloak must be
        // up-and-running and properly configured. https://github.com/Metaform/dataplane-sdk-net/issues/7 will add the appropriate sample.

        // services.AddAuthentication("KeycloakJWT")
        //     .AddJwtBearer("KeycloakJWT", options =>
        //     {
        //         // Configure Keycloak as the Identity Provider
        //         options.Authority = "http://localhost:8080/realms/master";
        //         options.RequireHttpsMetadata = false; // Only for develop
        //
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuer = true,
        //             ValidIssuer = "http://localhost:8080/realms/master",
        //             ValidateAudience = true,
        //             ValidAudience = "dataplane-api",
        //             ValidateIssuerSigningKey = true,
        //             ValidateLifetime = true,
        //             ValidateActor = false,
        //             ValidateTokenReplay = true
        //         };
        //     });

        // wire up ASP.net authorization handlers
        services.AddSdkAuthorization();
    }

    /// <summary>
    ///     Configures JWT Bearer authentication for the SDK API calls. This uses a symmetric key for signing, which is
    ///     configured in appsettings.*.json.
    ///     Please note that this is only for testing and demo purposes, in real-life scenarios a proper identity provider such
    ///     as Keycloak should be used.
    ///     DO NOT DO THIS IN PRODUCTION!
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <exception cref="InvalidOperationException">thrown if appsettings does not contain a Token:SecretKey entry</exception>
    private static void ConfigureExampleJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        // add authentication handler
        services.AddAuthentication("DataPlaneSdkJWT_example")
            .AddJwtBearer("DataPlaneSdkJWT_example", options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ??
                                                                        throw new InvalidOperationException("JwtSettings:SecretKey must not be empty"))),
                    ValidateLifetime = true,
                    ValidateActor = false,
                    ValidateTokenReplay = true
                };
            });
    }
}
