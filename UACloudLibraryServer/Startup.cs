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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text.Json;
    using Amazon.S3;
    using GraphQL.Server.Ui.Playground;
    using HotChocolate.AspNetCore;
    using HotChocolate.Data;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.AspNetCore.Authorization;
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
            services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);

            services.AddDefaultIdentity<IdentityUser>(options =>
                    //require confirmation mail if email sender API Key is set
                    options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["EmailSenderAPIKey"])
                    )
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IUserService, UserService>();

            services.AddTransient<IDatabase, CloudLibDataProvider>();

            if (!string.IsNullOrEmpty(Configuration["UseSendGridEmailSender"]))
            {
                services.AddTransient<IEmailSender, SendGridEmailSender>();
            }
            else
            {
                services.AddTransient<IEmailSender, PostmarkEmailSender>();
            }

            services.AddLogging(builder => builder.AddConsole());

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
                .AddOAuth("OAuth", "OPC Foundation", options =>
                {
                    options.AuthorizationEndpoint = "https://opcfoundation.org/oauth/authorize/";
                    options.TokenEndpoint = "https://opcfoundation.org/oauth/token/";
                    options.UserInformationEndpoint = "https://opcfoundation.org/oauth/me";

                    options.AccessDeniedPath = new PathString("/Account/AccessDenied");
                    options.CallbackPath = new PathString("/Account/ExternalLogin");

                    options.ClientId = Configuration["OAuth2ClientId"];
                    options.ClientSecret = Configuration["OAuth2ClientSecret"];

                    options.SaveTokens = true;

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "ID");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "user_email");

                    options.Events = new OAuthEvents {
                        OnCreatingTicket = async context =>
                        {
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

            services.AddAuthorization(options => {
                options.AddPolicy("ApprovalPolicy", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("UserAdministrationPolicy", policy => policy.RequireRole("Administrator"));
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

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "basic"
                                }
                            },
                            Array.Empty<string>()
                    }
                });

                options.CustomSchemaIds(type => type.ToString());

                options.EnableAnnotations();
            });

            services.AddSwaggerGenNewtonsoftSupport();

            // Setup file storage
            switch (Configuration["HostingPlatform"])
            {
                case "Azure": services.AddSingleton<IFileStorage, AzureFileStorage>(); break;
                case "AWS":
                    var awsOptions = Configuration.GetAWSOptions();
                    services.AddDefaultAWSOptions(awsOptions);
                    services.AddAWSService<IAmazonS3>();
                    services.AddSingleton<IFileStorage, AWSFileStorage>();
                    break;
                case "GCP": services.AddSingleton<IFileStorage, GCPFileStorage>(); break;
                case "DevDB": services.AddScoped<IFileStorage, DevDbFileStorage>(); break;
                default:
                {
                    services.AddSingleton<IFileStorage, LocalFileStorage>();
                    Console.WriteLine("WARNING: Using local filesystem for storage as HostingPlatform environment variable not specified or invalid!");
                    break;
                }
            }

            var serviceName = Configuration["Application"] ?? "UACloudLibrary";

            // setup data protection
            switch (Configuration["HostingPlatform"])
            {
                case "Azure": services.AddDataProtection().PersistKeysToAzureBlobStorage(Configuration["BlobStorageConnectionString"], "keys", Configuration["DataProtectionBlobName"]); break;
                case "AWS": services.AddDataProtection().PersistKeysToAWSSystemsManager($"/{serviceName}/DataProtection"); break;
                case "GCP": services.AddDataProtection().PersistKeysToGoogleCloudStorage(Configuration["BlobStorageConnectionString"], "DataProtectionProviderKeys.xml"); break;
                default: services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory())); break;
            }

            services.AddHttpContextAccessor();

            services.AddGraphQLServer()
                .AddAuthorization()
                .SetPagingOptions(new HotChocolate.Types.Pagination.PagingOptions {
                    IncludeTotalCount = true,
                    DefaultPageSize = 100,
                    MaxPageSize = 100,
                })
                .AddFiltering(fd => {
                    fd.AddDefaults().BindRuntimeType<UInt32, UnsignedIntOperationFilterInputType>();
                    fd.AddDefaults().BindRuntimeType<UInt32?, UnsignedIntOperationFilterInputType>();
                    fd.AddDefaults().BindRuntimeType<UInt16?, UnsignedShortOperationFilterInputType>();
                })
                .AddSorting()
                .AddQueryType<QueryModel>()
                .AddMutationType<MutationModel>()
                .AddType<CloudLibNodeSetModelType>()
                .BindRuntimeType<UInt32, HotChocolate.Types.UnsignedIntType>()
                .BindRuntimeType<UInt16, HotChocolate.Types.UnsignedShortType>()
                ;

            services.AddScoped<NodeSetModelIndexer>();
            services.AddScoped<NodeSetModelIndexerFactory>();

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
            appDbContext.Database.Migrate();

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

            app.UseGraphQLPlayground(
                "/graphqlui",
                new PlaygroundOptions() {
                    RequestCredentials = RequestCredentials.Include
                });
            app.UseGraphQLGraphiQL("/graphiql", new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions {
                ExplorerExtensionEnabled = true,

            });

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapGraphQL()
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "BasicAuthentication" })
                    .WithOptions(new GraphQLServerOptions {
                        EnableGetRequests = true,
                        Tool = { Enable = false },
                    });
            });
        }
    }
}
