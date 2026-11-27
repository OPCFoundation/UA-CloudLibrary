/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.OpenApi;
using Opc.Ua.Cloud.Library.Wot;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Opc.Ua.Cloud.Library.Swagger
{
    /// <summary>
    /// Adds the <c>application/tm+json</c> and <c>application/td+json</c> media types, each referencing
    /// a component schema of the W3C WoT document, to the 200 response of actions marked with
    /// <see cref="WotDocumentResponseAttribute"/>. The response's existing media types (the Cloud Library
    /// metadata) are kept.
    /// </summary>
    internal sealed class WotDocumentResponseOperationFilter : IOperationFilter
    {
        internal const string ThingModelSchemaId = "WotThingModel";
        internal const string ThingDescriptionSchemaId = "WotThingDescription";

        private const string SuccessStatusCode = "200";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo?.GetCustomAttribute<WotDocumentResponseAttribute>() == null)
            {
                return;
            }

            operation.Responses ??= new OpenApiResponses();
            if (!operation.Responses.TryGetValue(SuccessStatusCode, out IOpenApiResponse existing) || existing is not OpenApiResponse response)
            {
                response = new OpenApiResponse { Description = "Success" };
                operation.Responses[SuccessStatusCode] = response;
            }

            response.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal);
            response.Content[WotMediaTypes.ThingModel] = new OpenApiMediaType {
                Schema = Reference(context, ThingModelSchemaId,
                    "W3C Web of Things Thing Model (https://www.w3.org/TR/wot-thing-description11/#thing-model) generated "
                    + "losslessly from the NodeSet2 XML following the OPC UA WoT Binding; returned for nodesets that define types.",
                    "tm:ThingModel")
            };
            response.Content[WotMediaTypes.ThingDescription] = new OpenApiMediaType {
                Schema = Reference(context, ThingDescriptionSchemaId,
                    "W3C Web of Things Thing Description (https://www.w3.org/TR/wot-thing-description11/) generated "
                    + "losslessly from the NodeSet2 XML following the OPC UA WoT Binding; returned for nodesets whose "
                    + "projection root is an instance.",
                    null)
            };
        }

        private static OpenApiSchemaReference Reference(OperationFilterContext context, string id, string description, string type)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(id))
            {
                context.SchemaRepository.AddDefinition(id, DocumentSchema(description, type));
            }

            return new OpenApiSchemaReference(id, context.Document);
        }

        private static OpenApiSchema DocumentSchema(string description, string type)
        {
            Dictionary<string, IOpenApiSchema> properties = new(StringComparer.Ordinal) {
                ["@context"] = new OpenApiSchema {
                    Description = "JSON-LD context; includes the WoT Thing Description context and the OPC UA binding (uav) context."
                },
                ["@type"] = new OpenApiSchema {
                    Description = type == null ? "Semantic types of the Thing." : $"Semantic types of the Thing; includes '{type}'."
                },
                ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["properties"] = new OpenApiSchema { Type = JsonSchemaType.Object, AdditionalProperties = new OpenApiSchema() },
                ["actions"] = new OpenApiSchema { Type = JsonSchemaType.Object, AdditionalProperties = new OpenApiSchema() },
                ["events"] = new OpenApiSchema { Type = JsonSchemaType.Object, AdditionalProperties = new OpenApiSchema() },
            };

            return new OpenApiSchema {
                Type = JsonSchemaType.Object,
                Description = description,
                Required = new HashSet<string>(s_required, StringComparer.Ordinal),
                Properties = properties,
                AdditionalProperties = new OpenApiSchema(),
                ExternalDocs = new OpenApiExternalDocs {
                    Description = "W3C Web of Things (WoT) Thing Description 1.1",
                    Url = new Uri("https://www.w3.org/TR/wot-thing-description11/")
                }
            };
        }

        private static readonly string[] s_required = { "@context", "title" };
    }
}
