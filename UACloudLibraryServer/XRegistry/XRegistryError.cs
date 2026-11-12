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

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace Opc.Ua.Cloud.Library.XRegistry
{
    /// <summary>
    /// Builds xRegistry error responses. The body follows RFC 9457 (problem details) as required by
    /// the xRegistry core specification; <c>type</c> is the xRegistry error identifier URL.
    /// </summary>
    public static class XRegistryError
    {
        public const string TypePrefix = "https://xregistry.io/spec/core/errors/";

        public const string NotFound = "not_found";
        public const string UnknownId = "unknown_id";
        public const string MismatchedId = "mismatched_id";
        public const string InvalidData = "invalid_data";
        public const string BadFlag = "bad_flag";
        public const string BadRequest = "bad_request";
        public const string ActionNotSupported = "action_not_supported";
        public const string Conflict = "conflict";
        public const string Forbidden = "forbidden";
        public const string ServerError = "server_error";

        public static JsonObject Body(string errorId, int status, string title, string detail, string instance)
        {
            JsonObject body = new() {
                ["type"] = TypePrefix + errorId,
                ["status"] = status,
                ["title"] = title,
            };

            if (!string.IsNullOrEmpty(instance))
            {
                body["instance"] = instance;
            }

            if (!string.IsNullOrEmpty(detail))
            {
                body["detail"] = detail;
            }

            return body;
        }

        public static ObjectResult Result(string errorId, int status, string title, string detail, string instance)
        {
            return new ObjectResult(Body(errorId, status, title, detail, instance)) {
                StatusCode = status,
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
