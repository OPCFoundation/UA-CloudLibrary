
namespace UACloudLibrary
{
    using GraphQL.Server;
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
    using GraphQL.Server.Transports.AspNetCore;
    using UA_CloudLibrary.GraphQL.Types;
    using GraphQL;

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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
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

                c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authorization header using the Bearer scheme."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            // Setting up database context
            // Alternative would be to create queries manually
            services.AddDbContext<AppDbContext>(o =>
            {
                o.UseNpgsql(Configuration["ConnectionString"]);
            });
            // Setting up dependency injection
            services.AddSingleton<IServiceProvider>(c => new FuncServiceProvider(type => c.GetRequiredService(type)));
            services.Configure<IISServerOptions>(options => options.AllowSynchronousIO = true);

            // Setting up GraphQL with GraphQL DOTNET
            services
                .AddScoped<CloudLibQuery>()
                .AddScoped<Schema>()
                .AddGraphQL()
                .AddDefaultEndpointSelectorPolicy()
                // Add required services for GraphQL request/response de/serialization
                .AddSystemTextJson() // For .NET Core 3+
                .AddNewtonsoftJson() // For everything else
#if DEBUG
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
#endif                
                .AddDataLoader() // Add required services for DataLoader support
                .AddGraphTypes(typeof(Schema));
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
                endpoints.MapGraphQL<Schema, GraphQLHttpMiddleware<Schema>>();
                endpoints.MapControllers();
            });
        }
    }
}