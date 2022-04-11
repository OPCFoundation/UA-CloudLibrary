/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using Amazon.S3;
    using GraphQL;
    using GraphQL.DataLoader;
    using GraphQL.Execution;
    using GraphQL.Server;
    using GraphQL.Server.Ui.Playground;
    using GraphQL.SystemReactive;
    using Microsoft.AspNetCore.Authentication;
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
    using System;
    using System.IO;
    using UACloudLibrary.Interfaces;

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
            services.AddDbContext<AppDbContext>(o =>
            {
                o.UseNpgsql(PostgreSQLDB.CreateConnectionString());
            });

            services.AddDefaultIdentity<IdentityUser>(options => 
                    //require confirmation mail if sendgrid API Key is set
                    options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["SendGridAPIKey"])
                    ).AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IUserService, UserService>();

            services.AddSingleton<IDatabase, PostgreSQLDB>();

            services.AddTransient<IEmailSender, EmailSender>();

            services.AddLogging(builder => builder.AddConsole());

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            services.AddAuthorization();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "UA Cloud Library REST Service",
                    Version = "v1",
                    Description = "A REST-full interface to the CESMII & OPC Foundation Cloud Library",
                    Contact = new OpenApiContact
                    {
                        Name = "OPC Foundation",
                        Email = "office@opcfoundation.org",
                        Url = new Uri("https://opcfoundation.org/")
                    }
                });

                options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
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
                            new string[] {}
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
#if DEBUG
                default: services.AddSingleton<IFileStorage, LocalFileStorage>(); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }

            var serviceName = Configuration["Application"] ?? "UACloudLibrary";

            // setup data protection
            switch (Configuration["HostingPlatform"])
            {
                case "Azure": services.AddDataProtection().PersistKeysToAzureBlobStorage(Configuration["BlobStorageConnectionString"], "keys", "keys"); break;
                case "AWS": services.AddDataProtection().PersistKeysToAWSSystemsManager($"/{serviceName}/DataProtection"); break;
                case "GCP": services.AddDataProtection().PersistKeysToGoogleCloudStorage(Configuration["BlobStorageConnectionString"], "DataProtectionProviderKeys.xml"); break;
#if DEBUG
                default: services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory())); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }

            services.AddHttpContextAccessor();

            // setup GrapQL interface
            GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services)
                .AddSubscriptionDocumentExecuter()
                .AddServer(true)
                .AddSchema<UaCloudLibSchema>(GraphQL.DI.ServiceLifetime.Scoped)
                .ConfigureExecution(options =>
                {
                    options.EnableMetrics = Environment.IsDevelopment();
                    var logger = options.RequestServices.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = context => logger.LogError("{Error} occurred", context.OriginalException.Message);
                })
                .AddNewtonsoftJson()
                .AddErrorInfoProvider()
                .Configure<ErrorInfoProviderOptions>(options => options.ExposeExceptionStackTrace = Environment.IsDevelopment())
                .AddDataLoader()
                .AddGraphTypes(typeof(UaCloudLibSchema).Assembly)
                .AddUserContextBuilder(httpContext =>
                    new GraphQLUserContext
                    {
                        User = httpContext.User
                    }
            );

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
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

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UA Cloud Library REST Service");
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseGraphQL<UaCloudLibSchema, GraphQLUACloudLibMiddleware<UaCloudLibSchema>>();

            app.UseGraphQLPlayground(new PlaygroundOptions()
            {
                RequestCredentials = RequestCredentials.Include
            },
            "/graphqlui");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });
        }
    }
}