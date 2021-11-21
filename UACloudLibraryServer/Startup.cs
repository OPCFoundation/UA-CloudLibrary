
namespace UACloudLibrary
{
    using GraphQL;
    using GraphQL.EntityFramework;
    using GraphQL.Utilities;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UACloudLibrary.Interfaces;

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

            // Setup GraphQL
            GraphTypeTypeRegistry.Register<AddressSpaceCategory, AddressSpaceCategoryType>();
            GraphTypeTypeRegistry.Register<AddressSpaceNodeset2, AddressSpaceNodeset2Type>();
            GraphTypeTypeRegistry.Register<Organisation, OrganisationType>();
            GraphTypeTypeRegistry.Register<AddressSpace, AddressSpaceType>();
            GraphTypeTypeRegistry.Register<AddressSpaceLicense, AddressSpaceLicenseType>();

            services.AddSingleton<AddressSpace>();
            services.AddSingleton<AddressSpaceCategory>();
            services.AddSingleton<AddressSpaceNodeset2>();
            services.AddSingleton<AddressSpaceLicenseType>();

            EfGraphQLConventions.RegisterInContainer<AppDbContext>(services, null, GetGraphQLDBContext());
            EfGraphQLConventions.RegisterConnectionTypesInContainer(services);

            // Setup file storage
            foreach (var type in GetGraphQlTypes())
            {
                services.AddSingleton(type);
            }

            switch (Configuration["HostingPlatform"])
            {
                case "Azure": services.AddSingleton<IFileStorage, AzureFileStorage>(); break;
                case "AWS": services.AddSingleton<IFileStorage, AWSFileStorage>(); break;
                case "GCP": services.AddSingleton<IFileStorage, GCPFileStorage>(); break;
#if DEBUG
                default:
                    services.AddSingleton<IFileStorage, LocalFileStorage>(); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }

            services.AddAuthentication();

            services.AddAuthorization();

            services.AddDataProtection().PersistKeysToAzureBlobStorage(Configuration["BlobStorageConnectionString"], "keys", "keys");

            services.AddSingleton<IDocumentExecuter, EfDocumentExecuter>();
            services.AddSingleton<GraphQL.Types.ISchema, Schema>();

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        private static IModel GetGraphQLDBContext()
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(PostgreSQLDB.CreateConnectionString());
            using AppDbContext context = new AppDbContext(builder.Options);
            return context.Model;
        }

        private static IEnumerable<Type> GetGraphQlTypes()
        {
            return typeof(Startup).Assembly
                .GetTypes()
                .Where(x => !x.IsAbstract &&
                            (typeof(GraphQL.Types.IObjectGraphType).IsAssignableFrom(x) ||
                             typeof(GraphQL.Types.IInputObjectGraphType).IsAssignableFrom(x)));
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