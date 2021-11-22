
namespace UACloudLibrary
{
    using global::GraphQL.Server;
    using global::GraphQL.Server.Ui.Playground;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using AspNetCore.DataProtection.Aws.S3;
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
                case "AWS": services.AddSingleton<IFileStorage, AWSFileStorage>(); break;
                case "GCP": services.AddSingleton<IFileStorage, GCPFileStorage>(); break;
#if DEBUG
                default: services.AddSingleton<IFileStorage, LocalFileStorage>(); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }

            services.AddAuthentication();

            services.AddAuthorization();

            // setup data protection
            switch (Configuration["HostingPlatform"])
            {
                case "Azure": services.AddDataProtection().PersistKeysToAzureBlobStorage(Configuration["BlobStorageConnectionString"], "keys", "keys"); break;
                case "AWS": //TODO: Configure services.AddDataProtection().PersistKeysToAwsS3(); break;
                case "GCP": //TODO: Configure services.AddDataProtection().PersistKeysToGoogleCloudStorage(); break;
#if DEBUG
                default: services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory())); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }

            // setup GrapQL interface
            services.AddScoped<DatatypeModel>();
            services.AddScoped<DatatypeType>();

            services.AddScoped<MetadataModel>();
            services.AddScoped<MetadataType>();

            services.AddScoped<ObjecttypeModel>();
            services.AddScoped<ObjecttypeType>();

            services.AddScoped<ReferencetypeModel>();
            services.AddScoped<ReferencetypeType>();

            services.AddScoped<VariabletypeModel>();
            services.AddScoped<VariabletypeType>();

            services.AddScoped<UaCloudLibRepo>();
            services.AddScoped<UaCloudLibQuery>();
            services.AddScoped<UaCloudLibSchema>();

            services.AddHttpContextAccessor();

            services.AddGraphQL(options =>
            {
                options.EnableMetrics = true;
            })
            .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
            .AddNewtonsoftJson()
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

            app.UseGraphQL<UaCloudLibSchema>();

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