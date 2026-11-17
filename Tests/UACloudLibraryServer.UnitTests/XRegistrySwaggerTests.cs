using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using Opc.Ua.Cloud.Library.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    public class XRegistrySwaggerTests
    {
        [Fact]
        public void SwaggerDocumentGeneratesWithXRegistryController()
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment());
            services.AddMvcCore()
                .AddApiExplorer()
                .AddApplicationPart(typeof(XRegistryController).Assembly);
            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "test", Version = "v1" });
                options.CustomSchemaIds(type => type.ToString());
                options.EnableAnnotations();
            });

            using ServiceProvider provider = services.BuildServiceProvider();
            ISwaggerProvider swagger = provider.GetRequiredService<ISwaggerProvider>();

            OpenApiDocument document = swagger.GetSwagger("v1");

            Assert.Contains(document.Paths.Keys, p => p.StartsWith("/xregistry", System.StringComparison.Ordinal));
            Assert.Single(document.Paths.Keys, p => p.TrimEnd('/') == "/xregistry");
        }

        private sealed class FakeWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = System.IO.Path.GetTempPath();
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string ApplicationName { get; set; } = "UA-CloudLibrary";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
            public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
