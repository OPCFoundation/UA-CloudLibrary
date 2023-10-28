/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Cloud.Library.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// For use when the provider doesn't have a GraphQL interface and the downloading of nodesets
    /// </summary>
    internal class RestClient : IDisposable
    {
        private HttpClient client;

        public AuthenticationHeaderValue Authentication
        {
            set => client.DefaultRequestHeaders.Authorization = value;
            get => client.DefaultRequestHeaders.Authorization;
        }

        public RestClient(Uri address)
        {
            client = new HttpClient();
            client.BaseAddress = address;
        }

        public RestClient(string address, AuthenticationHeaderValue authentication)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(address);
            client.DefaultRequestHeaders.Authorization = authentication;
        }

        public RestClient(HttpClient httpClient)
        {
            client = httpClient;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        [Obsolete("Use GetBasicNodesetInformationAsync with offset and limit")]
        public async Task<List<UANodesetResult>> GetBasicNodesetInformationAsync(List<string> keywords = null)
        {
            var nodeSetResults = new List<UANodesetResult>();
            int offset = 0;
            int limit = 100;
            do
            {
                var results = await GetBasicNodesetInformationAsync(offset, limit, keywords).ConfigureAwait(false);
                nodeSetResults.AddRange(results);
                offset += limit;
            } while (nodeSetResults.Count == limit);
            return nodeSetResults;
        }

        public async Task<List<UANodesetResult>> GetBasicNodesetInformationAsync(int offset, int limit, List<string> keywords = null)
        {
            if (keywords == null)
            {
                keywords = new List<string>() { "*" };
            }

            // keywords are simply appended with "&keywords=UriEscapedKeyword2&keywords=UriEscapedKeyword3", etc.)
            var relativeUri = $"infomodel/find{PrepareArgumentsString(keywords)}&offset={offset}&limit={limit}";
            Uri address = new Uri(client.BaseAddress, relativeUri);
            HttpResponseMessage response = await client.GetAsync(address).ConfigureAwait(false);

            List<UANodesetResult> info = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                info = JsonConvert.DeserializeObject<List<UANodesetResult>>(responseJson);
            }

            return info;
        }

        public Task<UANameSpace> DownloadNodesetAsync(string identifier)
        {
            return DownloadNodesetAsync(identifier, false);
        }
        public async Task<UANameSpace> DownloadNodesetAsync(string identifier, bool metadataOnly)
        {
            var request = $"infomodel/download/{Uri.EscapeDataString(identifier)}";
            if (metadataOnly)
            {
                request += $"?{nameof(metadataOnly)}=true";
            }
            var address = new Uri(client.BaseAddress, request);
            HttpResponseMessage response = await client.GetAsync(address).ConfigureAwait(false);
            UANameSpace resultType = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                resultType = JsonConvert.DeserializeObject<UANameSpace>(responseJson, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc });
            }

            return resultType;
        }

        public async Task<(string NamespaceUri, string Identifier)[]> GetNamespaceIdsAsync()
        {
            Uri address = new Uri(client.BaseAddress, "infomodel/namespaces/");
            HttpResponseMessage response = await client.GetAsync(address).ConfigureAwait(false);
            (string namespaceUri, string identifier)[] resultType = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<string[]>(responseStr);
                resultType = result.Select(str => {
                    var parts = str.Split(',');
                    return (parts[0], parts[1]);
                }).ToArray();
            }
            return resultType;
        }

        private static string PrepareArgumentsString(List<string> arguments)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < arguments.Count; i++)
            {
                if (i == 0)
                {
                    stringBuilder.Append('?');
                }
                else
                {
                    stringBuilder.Append('&');
                }

                stringBuilder.Append("keywords=" + Uri.EscapeDataString(arguments[i]));
            }

            return stringBuilder.ToString();
        }

        internal async Task<(HttpStatusCode Status, string Message)> UploadNamespaceAsync(UANameSpace nameSpace, bool overwrite = false)
        {
            // upload infomodel to cloud library
            var uploadAddress = client.BaseAddress != null ?
                new Uri(client.BaseAddress, $"infomodel/upload{((overwrite) ? "?overwrite=true" : "")}") :
                null;

            HttpContent content = new StringContent(JsonConvert.SerializeObject(nameSpace), Encoding.UTF8, "application/json");

            var uploadResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, uploadAddress) { Content = content }).ConfigureAwait(false);
            var uploadResponseStr = await uploadResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (uploadResponse.StatusCode, $"{uploadResponseStr}");
        }
    }
}
