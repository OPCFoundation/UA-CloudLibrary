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
    using System.IO;
    using Amazon.S3;
    using GraphQL.Server.Ui.Playground;
    using HotChocolate.AspNetCore;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
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
                    //require confirmation mail if sendgrid API Key is set
                    options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["SendGridAPIKey"])
                    ).AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IDatabase, PostgreSQLDB>();

            services.AddTransient<IEmailSender, EmailSender>();

            services.AddLogging(builder => builder.AddConsole());

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            services.AddAuthorization();

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

                options.AddSecurityDefinition("basic", new OpenApiSecurityScheme {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authorization header using the Bearer scheme."
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
                case "Azure": services.AddDataProtection().PersistKeysToAzureBlobStorage(Configuration["BlobStorageConnectionString"], "keys", "keys"); break;
                case "AWS": services.AddDataProtection().PersistKeysToAWSSystemsManager($"/{serviceName}/DataProtection"); break;
                case "GCP": services.AddDataProtection().PersistKeysToGoogleCloudStorage(Configuration["BlobStorageConnectionString"], "DataProtectionProviderKeys.xml"); break;
                default: services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory())); break;
            }

            services.AddHttpContextAccessor();

            #region setup GraphQL server
            services.AddGraphQLServer()
                .AddAuthorization()
                .SetPagingOptions(new HotChocolate.Types.Pagination.PagingOptions {
                    IncludeTotalCount = true,
                    DefaultPageSize = 100,
                    MaxPageSize = 100,
                })
                .AddFiltering()
                .AddSorting()
                .AddQueryType<QueryModel>()
                .AddType<CloudLibNodeSetModelType>()
                .BindRuntimeType<UInt32, HotChocolate.Types.UnsignedIntType>()
                .BindRuntimeType<UInt16, HotChocolate.Types.UnsignedShortType>()
                ;
            services.AddScoped<NodeSetModelIndexer>();
            services.AddScoped<NodeSetModelIndexerFactory>();
            services.AddTransient<UaCloudLibResolver>();
            services.AddTransient<CloudLibDataProvider>();
            #endregion

            services.Configure<IISServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            #region setup Blazor pages
            services.AddServerSideBlazor();

            #endregion
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

            app.UseGraphQLPlayground(new PlaygroundOptions() {
                RequestCredentials = RequestCredentials.Include
            },
            "/graphqlui");
            app.UseGraphQLGraphiQL("/graphiql");

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
                        Tool = { Enable = false, },
                    })
                    ;
            });
        }
    }
}
