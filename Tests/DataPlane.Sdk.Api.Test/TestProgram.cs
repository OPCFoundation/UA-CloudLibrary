using DataPlane.Sdk.Api;
using DataPlane.Sdk.Api.Authorization.DataFlows;
using DataPlane.Sdk.Api.Controllers;
using DataPlane.Sdk.Api.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

const string testAuthScheme = "Test";

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddAuthentication(testAuthScheme)
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(testAuthScheme, _ => { });

services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = testAuthScheme;
    options.DefaultChallengeScheme = testAuthScheme;
});

services.AddControllers()
    .AddApplicationPart(typeof(DataPlaneSignalingApiController).Assembly);

services.AddSingleton<IAuthorizationHandler, DataFlowAuthorizationHandler>();

services.AddAuthorizationBuilder().AddPolicy("DataFlowAccess", delegate (AuthorizationPolicyBuilder policy) {
    policy.Requirements.Add(new DataFlowRequirement());
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
