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

namespace UACloudLibrary
{
    using GraphQL.Server;
    using GraphQL.Server.Ui.Playground;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using GoogleCloudStorage.AspNetCore.DataProtection;
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
    using UACloudLibrary.DbContextModels;
    using UACloudLibrary.Interfaces;
    using System.IO;
    using Amazon.S3;
    using GraphQL.EntityFramework;
    using GraphQL.Utilities;
    using GraphQL;
    using GraphQL.Types;
    using Microsoft.AspNetCore.Mvc;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<AppDbContext>();

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
                        Email = string.Empty,
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
                case "AWS":
                    services.AddDataProtection().PersistKeysToAWSSystemsManager($"/{serviceName}/DataProtection"); 
                    break;
                case "GCP": //TODO: Configure services.AddDataProtection().PersistKeysToGoogleCloudStorage(); break;
#if DEBUG
                default: services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory())); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }


            #region GraphQLEntitityFramework
            GraphTypeTypeRegistry.Register<MetadataModel, MetadataType>();
            GraphTypeTypeRegistry.Register<DatatypeModel, DatatypeType>();
            GraphTypeTypeRegistry.Register<ObjecttypeModel, ObjecttypeType>();
            GraphTypeTypeRegistry.Register<ReferencetypeModel, ReferencetypeType>();
            GraphTypeTypeRegistry.Register<VariabletypeModel, VariabletypeType>();
            GraphTypeTypeRegistry.Register<AddressSpaceLicense, AddressSpaceLicenseType>();

            EfGraphQLConventions.RegisterInContainer<AppDbContext>(
                    services,
                    model: AppDbContext.GetInstance());
            EfGraphQLConventions.RegisterConnectionTypesInContainer(services);
            #endregion

            // setup GrapQL interface
            services.AddSingleton<DatatypeModel>();
            services.AddSingleton<DatatypeType>();

            services.AddSingleton<MetadataModel>();
            services.AddSingleton<MetadataType>();

            services.AddSingleton<ObjecttypeModel>();
            services.AddSingleton<ObjecttypeType>();

            services.AddSingleton<ReferencetypeModel>();
            services.AddSingleton<ReferencetypeType>();

            services.AddSingleton<VariabletypeModel>();
            services.AddSingleton<VariabletypeType>();

            services.AddScoped<UaCloudLibRepo>();
            services.AddSingleton<UaCloudLibQuery>();
            //services.AddScoped<UaCloudLibSchema>();

            services.AddSingleton<IDocumentExecuter, EfDocumentExecuter>();
            services.AddSingleton<GraphQL.Types.ISchema, UaCloudLibSchema>();
            var mvc = services.AddMvc(option => option.EnableEndpointRouting = false);
            mvc.SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddHttpContextAccessor();

            //services.AddGraphQL(options =>
            //{
            //    options.EnableMetrics = true;
            //})
            //.AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
            //.AddNewtonsoftJson()
            //.AddUserContextBuilder(httpContext =>
            //    new GraphQLUserContext
            //    {
            //        User = httpContext.User
            //    }
            //);

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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

            //app.UseGraphQL<UaCloudLibSchema, GraphQLUACloudLibMiddleware<UaCloudLibSchema>>(path: "/graphql");

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

                //endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}