using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Opc.Ua.Cloud.Library.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Opc.Ua.Cloud.Library.Swagger
{
    internal sealed class NodesetValuesSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema is not OpenApiSchema valuesSchema
                || context.MemberInfo?.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType != typeof(NodesetValuesJsonConverter))
            {
                return;
            }

            valuesSchema.OneOf = [
                new OpenApiSchema {
                    Type = JsonSchemaType.Object,
                    AdditionalProperties = new OpenApiSchema()
                },
                // Preserve nullability on the typed branch so it also works in OpenAPI 3.0.
                new OpenApiSchema { Type = valuesSchema.Type }
            ];
            valuesSchema.Type = null;
        }
    }
}
