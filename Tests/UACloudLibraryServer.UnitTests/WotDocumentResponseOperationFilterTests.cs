using System.Collections.Generic;
using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.Swagger;
using Opc.Ua.Cloud.Library.Wot;

using Swashbuckle.AspNetCore.SwaggerGen;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    public class WotDocumentResponseOperationFilterTests
    {
        private static OperationFilterContext Context(MethodInfo method, SchemaRepository repository, OpenApiDocument document)
            => new(new ApiDescription(), null, repository, document, method);

        private static OpenApiOperation MetadataOperation()
        {
            return new OpenApiOperation {
                Responses = new OpenApiResponses {
                    ["200"] = new OpenApiResponse {
                        Description = "Metadata",
                        Content = new Dictionary<string, OpenApiMediaType> {
                            ["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.Object } }
                        }
                    }
                }
            };
        }

        [Fact]
        public void Apply_InfoModelDownload_AddsWotMediaTypesWithOwnSchemasAndKeepsMetadata()
        {
            MethodInfo download = typeof(InfoModelController).GetMethod(nameof(InfoModelController.DownloadNamespaceAsync));
            SchemaRepository repository = new();
            OpenApiDocument document = new();
            OpenApiOperation operation = MetadataOperation();

            new WotDocumentResponseOperationFilter().Apply(operation, Context(download, repository, document));

            IDictionary<string, OpenApiMediaType> content = ((OpenApiResponse)operation.Responses["200"]).Content;
            Assert.Contains("application/json", content.Keys);

            OpenApiSchemaReference thingModel = Assert.IsType<OpenApiSchemaReference>(content[WotMediaTypes.ThingModel].Schema);
            OpenApiSchemaReference thingDescription = Assert.IsType<OpenApiSchemaReference>(content[WotMediaTypes.ThingDescription].Schema);
            Assert.Equal(WotDocumentResponseOperationFilter.ThingModelSchemaId, thingModel.Reference.Id);
            Assert.Equal(WotDocumentResponseOperationFilter.ThingDescriptionSchemaId, thingDescription.Reference.Id);

            foreach (string id in new[] { WotDocumentResponseOperationFilter.ThingModelSchemaId, WotDocumentResponseOperationFilter.ThingDescriptionSchemaId })
            {
                OpenApiSchema schema = Assert.IsType<OpenApiSchema>(repository.Schemas[id]);
                Assert.Equal(JsonSchemaType.Object, schema.Type);
                Assert.Contains("@context", schema.Properties.Keys);
                Assert.Contains("title", schema.Required);
            }
        }

        [Fact]
        public void Apply_SecondOperation_ReusesRegisteredSchemas()
        {
            MethodInfo download = typeof(InfoModelController).GetMethod(nameof(InfoModelController.DownloadNamespaceAsync));
            SchemaRepository repository = new();
            OpenApiDocument document = new();
            WotDocumentResponseOperationFilter filter = new();

            filter.Apply(MetadataOperation(), Context(download, repository, document));
            filter.Apply(MetadataOperation(), Context(download, repository, document));

            Assert.Equal(2, repository.Schemas.Count);
        }

        [Fact]
        public void Apply_UnmarkedAction_LeavesResponsesUnchanged()
        {
            MethodInfo other = typeof(InfoModelController).GetMethod(nameof(InfoModelController.GetAllNamespacesandIdentifiersAsync));
            SchemaRepository repository = new();
            OpenApiOperation operation = MetadataOperation();

            new WotDocumentResponseOperationFilter().Apply(operation, Context(other, repository, new OpenApiDocument()));

            Assert.Single(((OpenApiResponse)operation.Responses["200"]).Content);
            Assert.Empty(repository.Schemas);
        }
    }
}
