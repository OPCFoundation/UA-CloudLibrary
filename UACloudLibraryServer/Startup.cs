/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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
using System.Security.Claims;
using System.Text.Json;
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
using Microsoft.OpenApi.Models;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.Authentication;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

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

            services.AddRazorPages();

            // Setup database context for ASP.NetCore Identity Scaffolding
            services.AddDbContext<AppDbContext>(
                options => options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)),
                ServiceLifetime.Transient);

            services.AddDefaultIdentity<IdentityUser>(options =>
                      //require confirmation mail if email sender API Key is set
                      options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["EmailSenderAPIKey"])
                    )
                .AddRoles<IdentityRole>()
#if APIKEY_AUTH
                .AddTokenProvider<ApiKeyTokenProvider>(ApiKeyTokenProvider.ApiKeyProviderName)
#endif
                .AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IUserService, UserService>();

            services.AddTransient<IDatabase, CloudLibDataProvider>();

            services.AddScoped<ICaptchaValidation, CaptchaValidation>();

            services.AddTransient<IEmailSender, PostmarkEmailSender>();

            services.AddLogging(builder => builder.AddConsole());

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, SignedInUserAuthenticationHandler>("SignedInUserAuthentication", null)
#if APIKEY_AUTH
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyAuthentication", null);
#endif

            //for captcha validation call
            //add httpclient service for dependency injection
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0
            services.AddHttpClient();

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
                options.AddPolicy("ApprovalPolicy", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("UserAdministrationPolicy", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("DeletePolicy", policy => policy.RequireRole("Administrator"));
            });

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

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
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
                    }
                });

#if APIKEY_AUTH
                options.AddSecurityDefinition("ApiKeyAuth", new OpenApiSecurityScheme {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-API-Key",
                    //Scheme = "basic"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKeyAuth"
                                }
                            },
                            Array.Empty<string>()
                    }
                });
#endif

                options.CustomSchemaIds(type => type.ToString());

                options.EnableAnnotations();
            });

            services.AddSwaggerGenNewtonsoftSupport();

            services.AddScoped<IFileStorage, DbFileStorage>();

            string serviceName = Configuration["Application"] ?? "UACloudLibrary";

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory()));

            services.AddScoped<NodeSetModelIndexer>();

            services.Configure<IISServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.AddServerSideBlazor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext appDbContext)
        {
            uint retryCount = 0;
            while (retryCount < 12)
            {
                try
                {
                    appDbContext.Database.Migrate();
                    break;
                }
                catch (SocketException)
                {
                    Console.WriteLine("Database not yet available or unknown, retrying...");
                    Task.Delay(5000).GetAwaiter().GetResult();
                    retryCount++;
                }
            }

            if (retryCount == 12)
            {
                // database permanently unavailable
                throw new InvalidOperationException("Database not available, exiting!");
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UA Cloud Library REST Service");
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

                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
            });
        }
    }
}
