using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Cloud.Library;
using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    public class InfoModelSwaggerTests
    {
        [Fact]
        public async Task SwaggerJsonDescribesUploadValuesAsObjectOrString()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                ApplicationName = typeof(Startup).Assembly.GetName().Name
            });
            var startup = new Startup(new ConfigurationBuilder().Build(), builder.Environment);
            startup.ConfigureServices(builder.Services);
            await using WebApplication app = builder.Build();

            RequestDelegate swagger = new ApplicationBuilder(app.Services).UseSwagger().Build();
            using var responseBody = new MemoryStream();
            var context = new DefaultHttpContext { RequestServices = app.Services };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/swagger/v1/swagger.json";
            context.Response.Body = responseBody;
            await swagger(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            responseBody.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync(responseBody, cancellationToken: TestContext.Current.CancellationToken);
            JsonElement root = document.RootElement;
            string requestSchemaRef = root.GetProperty("paths").GetProperty("/infomodel/upload").GetProperty("put")
                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString();
            Assert.Equal("#/components/schemas/Opc.Ua.Cloud.Library.Models.UANameSpaceUpload", requestSchemaRef);

            JsonElement upload = root.GetProperty("components").GetProperty("schemas")
                .GetProperty("Opc.Ua.Cloud.Library.Models.UANameSpaceUpload");
            JsonElement values = upload.GetProperty("properties").GetProperty("values");
            Assert.Equal(2, values.GetProperty("oneOf").GetArrayLength());
            Assert.False(values.TryGetProperty("type", out _));
            Assert.DoesNotContain(upload.GetProperty("required").EnumerateArray(), property => property.GetString() == "values");

            JsonElement objectSchema = values.GetProperty("oneOf")[0];
            Assert.Equal("object", objectSchema.GetProperty("type").GetString());
            Assert.Empty(objectSchema.GetProperty("additionalProperties").EnumerateObject());

            JsonElement stringSchema = values.GetProperty("oneOf")[1];
            Assert.Equal("string", stringSchema.GetProperty("type").GetString());
            Assert.True(stringSchema.GetProperty("nullable").GetBoolean());
        }
    }
}
