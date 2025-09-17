/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdminShell;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Opc.Ua.Cloud.Library.Authentication;
using Opc.Ua.Configuration;

[assembly: CLSCompliant(false)]
namespace Opc.Ua.Cloud.Library
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddNewtonsoftJson();

            services.AddRazorComponents().AddInteractiveServerComponents();

            // Setup database context for ASP.NetCore Identity Scaffolding
            services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);

            services.AddDefaultIdentity<IdentityUser>(options =>
                options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["EmailSenderAPIKey"])
            )
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddTokenProvider<ApiKeyTokenProvider>(ApiKeyTokenProvider.ApiKeyProviderName);

            services.AddScoped<UserService>();

            services.AddTransient<CloudLibDataProvider>();

            services.AddTransient<DbFileStorage>();

            services.AddTransient<UAClient>();

            services.AddSingleton<ApplicationInstance>();

            services.AddScoped<AssetAdministrationShellEnvironmentService>();

            services.AddScoped<CaptchaValidation>();

            if (!string.IsNullOrEmpty(Configuration["UseSendGridEmailSender"]))
            {
                services.AddTransient<IEmailSender, SendGridEmailSender>();
            }
            else
            {
                services.AddTransient<IEmailSender, PostmarkEmailSender>();
            }

            services.AddLogging(builder => builder.AddConsole());

            // for captcha validation call
            services.AddHttpClient();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, SignedInUserAuthenticationHandler>("SignedInUserAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyAuthentication", null);

            if (Configuration["Authentication:Microsoft:ClientId"] != null)
            {
                services.AddAuthentication()
                    .AddCookie()
                    .AddMicrosoftAccount(options => {
                        options.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                        options.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                    })
                    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
            }

            if (Configuration["OAuth2ClientId"] != null)
            {
                services.AddAuthentication()
                    .AddOAuth("OAuth", "OPC Foundation", options => {
                        options.AuthorizationEndpoint = "https://opcfoundation.org/oauth/authorize/";
                        options.TokenEndpoint = "https://opcfoundation.org/oauth/token/";
                        options.UserInformationEndpoint = "https://opcfoundation.org/oauth/me";

                        options.AccessDeniedPath = new PathString("/Account/AccessDenied");
                        options.CallbackPath = new PathString("/Account/ExternalLogin");

                        options.ClientId = Configuration["OAuth2ClientId"];
                        options.ClientSecret = Configuration["OAuth2ClientSecret"];

                        options.SaveTokens = true;

                        options.CorrelationCookie.SameSite = SameSiteMode.Strict;
                        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "ID");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "user_email");

                        options.Events = new OAuthEvents {
                            OnCreatingTicket = async context => {
                                List<AuthenticationToken> tokens = (List<AuthenticationToken>)context.Properties.GetTokens();

                                tokens.Add(new AuthenticationToken() {
                                    Name = "TicketCreated",
                                    Value = DateTime.UtcNow.ToString(DateTimeFormatInfo.InvariantInfo)
                                });

                                context.Properties.StoreTokens(tokens);

                                HttpResponseMessage response = await context.Backchannel.GetAsync($"{context.Options.UserInformationEndpoint}?access_token={context.AccessToken}").ConfigureAwait(false);
                                response.EnsureSuccessStatusCode();

                                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                JsonElement user = JsonDocument.Parse(json).RootElement;

                                context.RunClaimActions(user);
                            }
                        };
                    });
            }

            services.AddAuthorization(options => {
                options.AddPolicy("AdministrationPolicy", policy => policy.RequireRole("Administrator"));
            });

            if (Configuration["APIKeyAuth"] != null)
            {
                services.AddAuthorization(options => {
                    options.AddPolicy("ApiPolicy", policy => {
                        policy.AddAuthenticationSchemes("BasicAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("SignedInUserAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("ApiKeyAuthentication").RequireAuthenticatedUser();
                    });
                });
            }
            else
            {
                services.AddAuthorization(options => {
                    options.AddPolicy("ApiPolicy", policy => {
                        policy.AddAuthenticationSchemes("BasicAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("SignedInUserAuthentication").RequireAuthenticatedUser();
                    });
                });
            }

            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "UA Cloud Library REST Service",
                    Version = "v1",
                    Description = "A REST-full interface to the CESMII & OPC Foundation Cloud Library",
                    Contact = new OpenApiContact {
                        Name = "OPC Foundation",
                        Email = "office@opcfoundation.org",
                        Url = new Uri("https://opcfoundation.org/")
                    }
                });

                options.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "basicAuth"
                            }
                        },
                        Array.Empty<string>()
                }});

                if (Configuration["APIKeyAuth"] != null)
                {
                    options.AddSecurityDefinition("ApiKeyAuth", new OpenApiSecurityScheme {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "X-API-Key",
                        //Scheme = "basic"
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKeyAuth"
                            }
                        },
                        Array.Empty<string>()
                    }});
                }

                options.CustomSchemaIds(type => type.ToString());

                options.EnableAnnotations();
            });

            services.AddSwaggerGenNewtonsoftSupport();

            string serviceName = Configuration["Application"] ?? "UACloudLibrary";

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()));

            services.Configure<IISServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.AddServerSideBlazor();

            services.AddHostedService<CloudLibStartupTask>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext appDbContext, ApplicationInstance uaApp)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UA Cloud Library REST Service");
                c.EnablePersistAuthorization();
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapBlazorHub();

                endpoints.MapRazorPages();
            });
        }


        public class CloudLibStartupTask : IHostedService
        {
            private readonly IServiceProvider _serviceProvider;

            public CloudLibStartupTask(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var uaApp = scope.ServiceProvider.GetRequiredService<ApplicationInstance>();

                uint retryCount = 0;
                while (retryCount < 12)
                {
                    try
                    {
                        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Database not yet available or unknown, retrying...");
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                        retryCount++;
                    }
                }

                if (retryCount == 12)
                {
                    // database permanently unavailable
                    throw new InvalidOperationException("Database not available, exiting!");
                }

                await InitOPCUAClientServerAsync(uaApp).ConfigureAwait(false);
            }

            private async Task InitOPCUAClientServerAsync(ApplicationInstance uaApp)
            {
                try
                {
                    // wait 5 seconds for the HTTP server to complete starting up
                    // for Azure Container Apps, the HTTP server must be started before the OPC UA server
                    await Task.Delay(5000).ConfigureAwait(false);

                    // remove any existing certificate store
                    if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "pki")))
                    {
                        Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "pki"), true);
                    }

                    // load the application configuration
                    ApplicationConfiguration config = await uaApp.LoadApplicationConfiguration(Path.Combine(Directory.GetCurrentDirectory(), "Application.Config.xml"), false).ConfigureAwait(false);

                    // check the application certificate
                    await uaApp.CheckApplicationInstanceCertificates(false, 0).ConfigureAwait(false);

                    // create cert validator
                    config.CertificateValidator = new CertificateValidator();
                    config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                    await config.CertificateValidator.Update(config).ConfigureAwait(false);

                    Utils.Tracing.TraceEventHandler += new EventHandler<TraceEventArgs>(OpcStackLoggingHandler);

                    Console.WriteLine("OPC UA client/server app started.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("InitOPCUAClientServerAsync: " + ex.Message);
                    return;
                }
            }

            private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
            {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    // accept all OPC UA client certificates
                    e.Accept = true;
                }
            }

            private void OpcStackLoggingHandler(object sender, TraceEventArgs e)
            {
                ApplicationInstance app = sender as ApplicationInstance;
                if ((e.TraceMask & (Utils.TraceMasks.Error | Utils.TraceMasks.StackTrace | Utils.TraceMasks.StartStop | Utils.TraceMasks.ExternalSystem | Utils.TraceMasks.Security)) != 0)
                {
                    if (e.Exception != null)
                    {
                        Console.WriteLine("OPCUA: " + e.Exception.Message);
                        return;
                    }

                    if (!string.IsNullOrEmpty(e.Format))
                    {
                        Console.WriteLine("OPCUA: " + e.Format);
                    }

                    if (!string.IsNullOrEmpty(e.Message))
                    {
                        Console.WriteLine("OPCUA: " + e.Message);
                    }
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                // nothing to do
                return Task.CompletedTask;
            }
        }
    }
}
