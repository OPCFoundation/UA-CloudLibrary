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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// Reads nodeset values that are supplied either as a JSON object in the format
    /// {nodeId: value} or as a string containing that JSON, and exposes them as the JSON text.
    /// </summary>
    public class NodesetValuesJsonConverter : JsonConverter<string>
    {
        /// <inheritdoc/>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.StartObject:
                    // Keep the text exactly as it was sent. Re-serializing would silently drop
                    // duplicate property names, and the DPP access policy has to see those to
                    // reject an ambiguous controlledElements mapping.
                    return JsonElement.ParseValue(ref reader).GetRawText();

                default:
                    throw new JsonException("The values must be a JSON object in the format {nodeId: value} or a string containing that JSON.");
            }
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteStringValue(value);
        }
    }
}
