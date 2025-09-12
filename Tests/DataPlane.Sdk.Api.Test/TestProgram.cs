
namespace DataPlane.Sdk.Api.Test
{
    using System.Threading.Tasks;
    using DataPlane.Sdk.Api.Authorization.DataFlows;
    using DataPlane.Sdk.Api.Controllers;
    using DataPlane.Sdk.Core.Data;
    using DataPlane.Sdk.Core.Domain.Interfaces;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class TestProgram
    {
        public static async Task<TestServer> CreateTestServerAsync(DataFlowContext context, IDataPlaneSignalingService service)
        {
            IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services => {
                    const string testAuthScheme = "Test";

                    services.AddAuthentication(testAuthScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(testAuthScheme, _ => { });

                    services.Configure<AuthenticationOptions>(options => {
                        options.DefaultAuthenticateScheme = testAuthScheme;
                        options.DefaultChallengeScheme = testAuthScheme;
                    });

                    services.AddControllers()
                        .AddApplicationPart(typeof(DataPlaneSignalingApiController).Assembly);

                    services.AddSingleton<IAuthorizationHandler, DataFlowAuthorizationHandler>();
                    services.AddSingleton<IDataPlaneStore>(context);
                    services.AddSingleton(service);

                    services.AddAuthorizationBuilder().AddPolicy("DataFlowAccess", policy => {
                        //policy.Requirements.Add(new DataFlowRequirement());
                        policy.RequireAuthenticatedUser();
                    });
                });

                webBuilder.Configure(app => {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });
            });

            IHost host = await hostBuilder.StartAsync().ConfigureAwait(false);

            return host.GetTestServer();
        }
    }
}
