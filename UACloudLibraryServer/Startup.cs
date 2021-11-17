
namespace UACloudLibrary
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using System;
    using UACloudLibrary.Interfaces;
    using UA_CloudLibrary.GraphQL;
    using Microsoft.EntityFrameworkCore;
    using GraphQL;
    using GraphQL.Utilities;
    using GraphQL.EntityFramework;
    using UA_CloudLibrary.GraphQL.GraphTypes;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;

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
            services.AddControllers();

            services.AddScoped<IUserService, UserService>();

            services.AddAuthentication("BasicAuthentication")
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
                        Url = new Uri("https://opcfoundation.org/"),
                    },
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
            });

            // Defining which dotnet class represents which GraphQL type class
            GraphTypeTypeRegistry.Register<AddressSpaceCategory, AddressSpaceCategoryType>();
            GraphTypeTypeRegistry.Register<AddressSpaceNodeset2, AddressSpaceNodeset2Type>();
            GraphTypeTypeRegistry.Register<Organisation, OrganisationType>();
            GraphTypeTypeRegistry.Register<AddressSpace, AddressSpaceType>();
            GraphTypeTypeRegistry.Register<AddressSpaceLicense, AddressSpaceLicenseType>();

            EfGraphQLConventions.RegisterInContainer<AppDbContext>(
                    services,
                    model: AppDbContext.GetInstance());
            EfGraphQLConventions.RegisterConnectionTypesInContainer(services);

            foreach (var type in GetGraphQlTypes())
            {
                services.AddSingleton(type);
            }

            services.AddSingleton<AddressSpace>();
            services.AddSingleton<AddressSpaceCategory>();
            services.AddSingleton<AddressSpaceNodeset2>();
            services.AddSingleton<AddressSpaceLicenseType>();
            services.AddSingleton<DatatypeGQL>();
            services.AddSingleton<MetadataGQL>();
            services.AddSingleton<NodesetGQL>();
            services.AddSingleton<ObjecttypeGQL>();
            services.AddSingleton<ReferencetypeGQL>();
            services.AddSingleton<VariabletypeGQL>();

            // Setting up database context
            services.AddDbContext<AppDbContext>(o =>
            {
                // Obtain connection string information from the environment
                string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
                string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
                string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

                string DBname = "uacloudlib";
                string Port = "5432";

                // Build connection string using parameters from portal
                string connectionString = string.Format(
                    "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                    Host,
                    User,
                    DBname,
                    Port,
                    Password);

                o.UseNpgsql(connectionString);
            });

            services.AddSingleton<IDocumentExecuter, EfDocumentExecuter>();
            services.AddSingleton<GraphQL.Types.ISchema, Schema>();

            var mvc = services.AddMvc(option => option.EnableEndpointRouting = false);
            mvc.SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.Configure<IISServerOptions>(options => options.AllowSynchronousIO = true);

            services.AddControllers().AddNewtonsoftJson();
        }

        static IEnumerable<Type> GetGraphQlTypes()
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

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}